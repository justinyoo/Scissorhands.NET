﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNet.Http;
using Microsoft.Extensions.PlatformAbstractions;

using Newtonsoft.Json;

using Scissorhands.Helpers;
using Scissorhands.Models.Posts;
using Scissorhands.Models.Settings;
using Scissorhands.Services.Exceptions;

namespace Scissorhands.Services
{
    /// <summary>
    /// This represents the service entity for publish.
    /// </summary>
    public class PublishService : IPublishService
    {
        private const string MediaType = "application/json";
        private const string CharSet = "utf-8";
        private const string PostPublishHtml = "/admin/post/publish/html";

        private readonly WebAppSettings _settings;
        private readonly IMarkdownHelper _markdownHelper;
        private readonly IFileHelper _fileHelper;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishService"/> class.
        /// </summary>
        /// <param name="settings"><see cref="WebAppSettings"/> instance.</param>
        /// <param name="markdownHelper"><see cref="IMarkdownHelper"/> instance.</param>
        /// <param name="fileHelper"><see cref="IFileHelper"/> instance.</param>
        public PublishService(WebAppSettings settings, IMarkdownHelper markdownHelper, IFileHelper fileHelper)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this._settings = settings;

            if (markdownHelper == null)
            {
                throw new ArgumentNullException(nameof(markdownHelper));
            }

            this._markdownHelper = markdownHelper;

            if (fileHelper == null)
            {
                throw new ArgumentNullException(nameof(fileHelper));
            }

            this._fileHelper = fileHelper;
        }

        /// <summary>
        /// Publishes the markdown as a file.
        /// </summary>
        /// <param name="markdown">Content in Markdown format.</param>
        /// <param name="env"><see cref="IApplicationEnvironment"/> instance.</param>
        /// <returns>Returns the Markdown file path in a virtual path format.</returns>
        public async Task<string> PublishMarkdownAsync(string markdown, IApplicationEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                throw new ArgumentNullException(nameof(markdown));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            var filename = "markdown.md";
            var markdownpath = $"{this._settings.MarkdownPath}/{filename}";

            var filepath = this._fileHelper.GetDirectory(env, this._settings.MarkdownPath);
            filepath = Path.Combine(new[] { filepath, filename });

            var written = await this._fileHelper.WriteAsync(filepath, markdown).ConfigureAwait(false);
            if (!written)
            {
                throw new PublishFailedException("Markdown not published");
            }

            return markdownpath;
        }

        /// <summary>
        /// Publishes the HTML post as a file.
        /// </summary>
        /// <param name="html">Content in HTML format.</param>
        /// <param name="env"><see cref="IApplicationEnvironment"/> instance.</param>
        /// <returns>Returns the HTML file path.</returns>
        public async Task<string> PublishHtmlAsync(string html, IApplicationEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentNullException(nameof(html));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            var filename = "post.html";
            var htmlpath = $"{this._settings.HtmlPath}/{filename}";

            var filepath = this._fileHelper.GetDirectory(env, this._settings.HtmlPath);
            filepath = Path.Combine(new[] { filepath, filename });

            var written = await this._fileHelper.WriteAsync(filepath, html).ConfigureAwait(false);
            if (!written)
            {
                throw new PublishFailedException("Post not published");
            }

            return htmlpath;
        }

        /// <summary>
        /// Publishes the post as a file.
        /// </summary>
        /// <param name="markdown">Content in Markdown format.</param>
        /// <param name="env"><see cref="IApplicationEnvironment"/> instance.</param>
        /// <param name="request"><see cref="HttpRequest"/> instance.</param>
        /// <returns>Returns the <see cref="PublishedPostPath"/> instance containing paths for published files.</returns>
        public async Task<PublishedPostPath> PublishPostAsync(string markdown, IApplicationEnvironment env, HttpRequest request)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                throw new ArgumentNullException(nameof(markdown));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            var publishedpath = new PublishedPostPath();

            var markdownpath = await this.PublishMarkdownAsync(markdown, env).ConfigureAwait(false);
            publishedpath.Markdown = markdownpath;

            var html = await this.GetPublishedHtmlAsync(markdown, request).ConfigureAwait(false);

            var htmlpath = await this.PublishHtmlAsync(html, env).ConfigureAwait(false);
            publishedpath.Html = htmlpath;

            return publishedpath;
        }

        /// <summary>
        /// Gets the published HTML content.
        /// </summary>
        /// <param name="markdown">Content in Markdown format.</param>
        /// <param name="request"><see cref="HttpRequest"/> instance.</param>
        /// <returns>Returns the published HTML content.</returns>
        public async Task<string> GetPublishedHtmlAsync(string markdown, HttpRequest request)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                throw new ArgumentNullException(nameof(markdown));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var parsedHtml = this._markdownHelper.Parse(markdown);

            var publishing = new PublishedContent() { Theme = this._settings.Theme, Markdown = markdown, Html = parsedHtml };

            using (var client = CreateHttpClient(request))
            using (var content = CreateStringContent(publishing))
            {
                var response = await client.PostAsync(PostPublishHtml, content).ConfigureAwait(false);
                var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return html;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
        }

        private static HttpClient CreateHttpClient(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var url = string.Join("://", request.IsHttps ? "https" : "http", request.Host.Value);
            var baseAddressUri = new Uri(url);
            var client = new HttpClient() { BaseAddress = baseAddressUri };
            return client;
        }

        private static StringContent CreateStringContent(PublishedContent publishing)
        {
            if (publishing == null)
            {
                throw new ArgumentNullException(nameof(publishing));
            }

            var content = new StringContent(JsonConvert.SerializeObject(publishing), Encoding.UTF8)
            {
                Headers =
                                      {
                                          ContentType = new MediaTypeHeaderValue(MediaType)
                                                            {
                                                                CharSet = CharSet
                                                            }
                                      }
            };
            return content;
        }
    }
}