/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

namespace WebHooksSample
{
    using Microsoft.Azure.WebJobs;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// StreamBinder will be used to read/write streams from azure blob storage
    /// </summary>
    public class StreamBinder : ICloudBlobStreamBinder<string>
    {
        /// <summary>
        /// Reads a stream objects and converts to a string.
        /// </summary>
        /// <param name="input">A Stream input.</param>
        /// <param name="cancellationToken">A cancellation token that can be used for long executing functions, in case webjobs is shutting down.</param>
        /// <returns>string representation of stream</returns>
        public Task<string> ReadFromStreamAsync(Stream input, CancellationToken cancellationToken)
        {
            if (input == null)
            {
                return Task.FromResult<string>(null);
            }

            using (StreamReader reader = new StreamReader(input))
            {
                return Task.FromResult(reader.ReadToEnd());
            }
        }

        /// <summary>
        /// Writes string object to an output stream.
        /// </summary>
        /// <param name="value">Input string value</param>
        /// <param name="output">Output stream.</param>
        /// <param name="cancellationToken">A cancellation token that can be used for long executing functions, in case webjobs is shutting down.</param>
        /// <returns></returns>
        public Task WriteToStreamAsync(string value, Stream output, CancellationToken cancellationToken)
        {
            using (StreamWriter writer = new StreamWriter(output))
            {
                writer.Write(value);
            }

            return Task.FromResult(0);
        }
    }
}
