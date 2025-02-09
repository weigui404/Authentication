// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Backup;
using Stratum.Core.Converter;
using Moq;
using Stratum.Test.Converter.Fixture;
using Xunit;

namespace Stratum.Test.Converter
{
    public class KeePassBackupConverterTest : IClassFixture<KeePassBackupFixture>
    {
        private readonly KeePassBackupFixture _keePassBackupFixture;
        private readonly KeePassBackupConverter _keePassBackupConverter;

        public KeePassBackupConverterTest(KeePassBackupFixture keePassBackupFixture)
        {
            _keePassBackupFixture = keePassBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _keePassBackupConverter = new KeePassBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync_ok()
        {
            var variants = new[]
            {
                _keePassBackupFixture.Aes256Argon2dData, _keePassBackupFixture.Aes256Argon2IdData,
                _keePassBackupFixture.Aes256AesKdfData, _keePassBackupFixture.ChaCha20Argon2dData,
                _keePassBackupFixture.TwoFishArgon2dData, _keePassBackupFixture.NoCompressionData,
                _keePassBackupFixture.BigData, _keePassBackupFixture.NoRecycleBinData
            };

            foreach (var variant in variants)
            {
                var result = await _keePassBackupConverter.ConvertAsync(variant, "test");

                Assert.Empty(result.Failures);

                Assert.Equal(7, result.Backup.Authenticators.Count());
                Assert.Null(result.Backup.Categories);
                Assert.Null(result.Backup.AuthenticatorCategories);
                Assert.Null(result.Backup.CustomIcons);
            }
        }

        [Fact]
        public async Task ConvertAsync_wrongPassword()
        {
            var variants = new[]
            {
                _keePassBackupFixture.Aes256Argon2dData, _keePassBackupFixture.Aes256Argon2IdData,
                _keePassBackupFixture.Aes256AesKdfData, _keePassBackupFixture.ChaCha20Argon2dData,
                _keePassBackupFixture.TwoFishArgon2dData, _keePassBackupFixture.NoCompressionData,
                _keePassBackupFixture.BigData, _keePassBackupFixture.NoRecycleBinData
            };

            foreach (var variant in variants)
            {
                await Assert.ThrowsAsync<BackupPasswordException>(() =>
                    _keePassBackupConverter.ConvertAsync(variant, "testing"));
            }
        }

        [Fact]
        public async Task ConvertAsync_kdbx3()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _keePassBackupConverter.ConvertAsync(_keePassBackupFixture.Kdbx3Data, "test"));
        }

        [Fact]
        public async Task ConvertAsync_recycleBin()
        {
            var result = await _keePassBackupConverter.ConvertAsync(_keePassBackupFixture.RecycleBinData, "test");

            Assert.Empty(result.Failures);
            Assert.Empty(result.Backup.Authenticators);
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}