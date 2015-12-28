﻿using System;
using System.IO;
using System.Threading.Tasks;

using Aliencube.Scissorhands.Services.Helpers;
using Aliencube.Scissorhands.Services.Tests.Fixtures;

using FluentAssertions;

using Xunit;

namespace Aliencube.Scissorhands.Services.Tests
{
    /// <summary>
    /// This represents the test entity for the <see cref="FileHelper"/> class.
    /// </summary>
    public class FileHelperTest : IClassFixture<FileHelperFixture>
    {
        private readonly IFileHelper _helper;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHelperTest"/> class.
        /// </summary>
        /// <param name="fixture"><see cref="FileHelperFixture"/> instance.</param>
        public FileHelperTest(FileHelperFixture fixture)
        {
            this._helper = fixture.FileHelper;
        }

        /// <summary>
        /// Tests whether the given file path returns null or not.
        /// </summary>
        /// <param name="filepath">Fully qualified file path.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async void Given_Filepath_ShouldReturn_Null(string filepath)
        {
            var result = await this._helper.ReadAsync(filepath).ConfigureAwait(false);
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests whether the given file path returns content or not.
        /// </summary>
        [Fact]
        public async void Given_Filepath_ShouldReturn_Content()
        {
            var filepath = "project.json";
            var result = await this._helper.ReadAsync(filepath).ConfigureAwait(false);
            result.Should().StartWithEquivalent("{");
        }

        /// <summary>
        /// Tests whether the null file path should throw an <see cref="ArgumentNullException"/> or not.
        /// </summary>
        [Fact]
        public void Given_NullFilepath_ShouldThrow_ArgumentNullException()
        {
            Func<Task> func = async () => { await this._helper.WriteAsync(null, string.Empty).ConfigureAwait(false); };
            func.ShouldThrow<ArgumentNullException>();
        }

        /// <summary>
        /// Tests whether the null value should return <c>False</c> or not.
        /// </summary>
        /// <param name="value">Content value.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async void Given_NullContent_ShouldReturn_False(string value)
        {
            var result = await this._helper.WriteAsync("test.bak", value).ConfigureAwait(false);
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests whether the null value should return <c>True</c> or not.
        /// </summary>
        /// <param name="value">Content value.</param>
        [Theory]
        [InlineData("**Hello World**")]
        public async void Given_Content_ShouldReturn_True(string value)
        {
            var filepath = "test.bak";
            var result = await this._helper.WriteAsync(filepath, value).ConfigureAwait(false);
            result.Should().BeTrue();

            var exists = File.Exists(filepath);
            exists.Should().BeTrue();

            var content = await this._helper.ReadAsync(filepath).ConfigureAwait(false);
            content.Should().Be(value);

            if (exists)
            {
                File.Delete(filepath);
            }
        }
    }
}