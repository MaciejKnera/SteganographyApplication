using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MVCUI.Models;
using SteganographyLogic.Enums;
using SteganographyLogic.Helpers;
using SteganographyLogic.Processors;

namespace MVCUI.Controllers
{
    public class AudioController : Controller
    {
        private readonly IAudioProcessor _audioProcessor;
        private readonly IInputValidation _inputValidation;

        public AudioController(IAudioProcessor audioProcessor, IInputValidation inputValidation)
        {
            AudioForm = new InputForm();
            _audioProcessor = audioProcessor;
            _inputValidation = inputValidation;
        }

        [BindProperty]
        public InputForm AudioForm { get; set; }

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
                byte[] carrierAudio = null;
                byte[] message = null;
                byte[] output;

                if (files.Count > 0)
                {
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memomyStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memomyStream);
                            carrierAudio = memomyStream.ToArray();
                        }
                    }

                    if (_audioProcessor.CheckIfFileIsWaveType(carrierAudio) == false)
                    {
                        return NotFound((Json(new { error = "wrongFormat" })));
                    }

                }
                else
                {
                    return NotFound();
                }

                if (files.Count < 2 && AudioForm.Message != null)
                {
                    message = Encoding.ASCII.GetBytes(AudioForm.Message);

                    if (_inputValidation.IsAudioValid(carrierAudio, message) == false)
                    {
                        return NotFound(Json(new { error = "fileTooBig" }));
                    }

                    output = _audioProcessor.HideMessage(carrierAudio, message);
                }
                else if (files.Count > 1 && AudioForm.Message  == null)
                {
                    using (var fileStream = files[1].OpenReadStream())
                    {
                        using (var memomyStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memomyStream);
                            message = memomyStream.ToArray();
                        }
                    }

                    if (_inputValidation.IsAudioValid(carrierAudio, message, files[1].FileName) == false)
                    {
                        return NotFound(Json(new { error = "fileTooBig" }));
                    }

                    output = _audioProcessor.HideMessage(carrierAudio, message, files[1].FileName);
                }
                else
                {
                    return NotFound();
                }

                string base64Audio = "data:audio/wav;base64," + Convert.ToBase64String(output);
                return Json(new { source = base64Audio, fileName = Path.GetFileNameWithoutExtension(files[0].FileName) });

            }

            return NotFound();
        }


        [HttpGet]
        public IActionResult Decode()
        {
            return View();
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public IActionResult DecodePost()
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                byte[] byteAudio = null;

                if (files.Count > 0)
                {
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memoryStream);
                            byteAudio = memoryStream.ToArray();
                        }
                    }

                    if (_audioProcessor.CheckIfFileIsWaveType(byteAudio) == false)
                    {
                        return NotFound(Json(new { error = "wrongFormat" }));
                    }
                }
                else
                {
                    return NotFound();
                }

                string fileName;
                bool containsMessage;
                byte[] decodedMessage = _audioProcessor.GetMessage(byteAudio, out fileName, out containsMessage);

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
