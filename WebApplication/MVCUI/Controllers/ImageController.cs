using Microsoft.AspNetCore.Mvc;
using MVCUI.Models;
using SteganographyLogic.Enums;
using SteganographyLogic.Helpers;
using SteganographyLogic.Processors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MVCUI.Controllers
{
    public class ImageController : Controller
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly IInputValidation _inputValidation;

        public ImageController(IImageProcessor imageProcessor, IInputValidation inputValidation)
        {
            ImageForm = new InputForm();
            _imageProcessor = imageProcessor;
            _inputValidation = inputValidation;
        }

        [BindProperty]
        public InputForm ImageForm { get; set; }

        [HttpGet]
        public IActionResult Encode()
        {
            return View();
        }

        [HttpPost]
        public IActionResult EncodePost()
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                byte[] carrierImage = null;
                byte[] message = null;
                byte[] output = null;

                if (files.Count > 0)
                {
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memomyStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memomyStream);
                            carrierImage = memomyStream.ToArray();
                        }
                    }
                }
                else
                {
                    return NotFound();
                }

                if (files.Count < 2 && ImageForm.Message != null)
                {
                    message = Encoding.ASCII.GetBytes(ImageForm.Message);

                    if (_inputValidation.IsImageVaild(carrierImage, message) == false)
                    {
                        return NotFound(Json(new { invalid = true }));
                    }

                    output = _imageProcessor.HideMessage(carrierImage, message);
                }
                else if (files.Count > 1 && ImageForm.Message == null)
                {
                    using (var fileStream = files[1].OpenReadStream())
                    {
                        using (var memomyStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memomyStream);
                            message = memomyStream.ToArray();
                        }
                    }

                    if (_inputValidation.IsImageVaild(carrierImage, message, files[1].FileName) == false)
                    {
                        return NotFound(Json(new { invalid = true }));
                    }

                    output = _imageProcessor.HideMessage(carrierImage, message, files[1].FileName);
                }
                else
                {
                    return NotFound();
                }
                string base64Image = "data:image/png;base64," + Convert.ToBase64String(output);
                return Json(new { source = base64Image, fileName = Path.GetFileNameWithoutExtension(files[0].FileName) });

            }

            return NotFound();
        }


        [HttpGet]
        public IActionResult Decode()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DecodePost()
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                byte[] byteImage = null;

                if (files.Count > 0)
                {
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memoryStream);
                            byteImage = memoryStream.ToArray();
                        }
                    }
                }
                else
                {
                    return NotFound();
                }

                string fileName;
                bool containsMessage;
                byte[] decodedMessage =  _imageProcessor.GetMessage(byteImage, out fileName, out containsMessage);

                if (containsMessage == false)
                {
                    return NotFound(Json(new { error = "message not found" }));
                }

                if (fileName == null && containsMessage)
                {
                    return Json(new { message = Encoding.ASCII.GetString(decodedMessage), type = MessageType.String });
                }
                else if (fileName != null && containsMessage)
                {
                    string extension = Path.GetExtension(fileName).Replace(".", "");
                    string mimeType = string.Empty;

                    foreach (KeyValuePair<string, string> item in StaticData.mimeTypes)
                    {
                        if (item.Key.Contains(extension, StringComparison.InvariantCultureIgnoreCase))
                        {
                            mimeType = item.Value;
                        }
                    }

                    if (Enum.GetNames(typeof(TextFileType)).Any(x => x.ToLowerInvariant() == extension.ToLowerInvariant()))
                    { 
                        return Json(new { message = decodedMessage, fileName = fileName, type = MessageType.Txt, mimeType = mimeType });
                    }
                    else if (Enum.GetNames(typeof(ImageType)).Any(x => x.ToLowerInvariant() == extension.ToLowerInvariant()))
                    {
                        string data = "data:" + mimeType + ";base64," + Convert.ToBase64String(decodedMessage);
                        return Json(new { message = data, fileName = fileName, type = MessageType.Image });
                    }
                    else if (Enum.GetNames(typeof(AudioType)).Any(x => x.ToLowerInvariant() == extension.ToLowerInvariant()))
                    {
                        string data = "data:" + mimeType + ";base64," + Convert.ToBase64String(decodedMessage);
                        return Json(new { message = data, fileName = fileName, type = MessageType.Audio, mimeType = mimeType });
                    }
                    else
                    {
                        return Json(new { type = MessageType.Empty });
                    }

                }
            }

            return NotFound();
        }
    }
}
