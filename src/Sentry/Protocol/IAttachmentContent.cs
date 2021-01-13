﻿using System.IO;

namespace Sentry.Protocol
{
    /// <summary>
    /// Attachment content.
    /// </summary>
    public interface IAttachmentContent
    {
        /// <summary>
        /// Gets the stream that represents attachment content.
        /// </summary>
        Stream GetStream();
    }
}
