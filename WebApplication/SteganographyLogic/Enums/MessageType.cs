using System;
using System.Collections.Generic;
using System.Text;

namespace SteganographyLogic.Enums
{
    public enum MessageType
    {
        String,
        Txt,
        Image,
        Audio,
        Empty
    }

    public enum TextFileType
    {
        Txt,
        Json,
        Js,
        Html,
        Htm,
        Py,
        Css,
        Doc,
        Docx,
        Odt,
        Pdf,
        Rtf,
        Tex,
        Bat,
        Xml,
        Empty
    }

    public enum ImageType
    {
        Bmp,
        Emf,
        Gif,
        Icon,
        Jpeg,
        Png,
        Tiff,
        Wmf,
        Jpg,
        Svg,
    }

    public enum AudioType
    {
        Aiff,
        Aif,
        Aifc,
        Au,
        Aac,
        Avi,
        Flac,
        M4a,
        M3u,
        M4b,
        M4p,
        M4u,
        Mp3,
        Mp4,
        Ogg,
        Wav,
        Wma,
    }
}
