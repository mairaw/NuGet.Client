// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.CommandLine.Test;
using NuGet.CommandLine.Test.Caching;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.PackageExtraction;
using NuGet.Packaging.Signing;
using NuGet.Test.Utility;
using Test.Utility.Signing;
using Xunit;
namespace NuGet.CommandLine.FuncTest.Commands
{
    /// <summary>
    /// Tests Sign command
    /// These tests require admin privilege as the certs need to be added to the root store location
    /// </summary>
    [Collection(SignCommandTestCollection.Name)]
    public class InstallCommandTests
    {
        private SignCommandTestFixture _testFixture;
        private TrustedTestCert<TestCertificate> _trustedTestCert;
        private TrustedTestCert<TestCertificate> _trustedRepoTestCert;
        private TrustedTestCert<TestCertificate> _trustedRepoSSLCert;
        private string _nugetExePath;

        public InstallCommandTests(SignCommandTestFixture fixture)
        {
            _testFixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _trustedTestCert = _testFixture.TrustedTestCertificate;
            _nugetExePath = _testFixture.NuGetExePath;
            _trustedRepoTestCert = SigningTestUtility.GenerateTrustedTestCertificate();
            _trustedRepoSSLCert = SigningTestUtility.GenerateTrustedTestSSLCertificate();
        }

        [CIOnlyFact]
        public async Task InstallPackage_RepositorySignedPackage_PackageSignedWithCertFromRepositoryCertificateList_SuccessAsync()
        {
            Debugger.Launch();

            // Arrange
            using (var dir = TestDirectory.Create())
            using (var repoCertificate = new X509Certificate2(_trustedRepoTestCert.Source.Cert))
            using (var repoSSLCertificate = new X509Certificate2(_trustedRepoSSLCert.Source.Cert))
            using (var mockServer = new MockServer())
            {
                // Arrange
                var baseUrl = mockServer.Uri.TrimEnd(new[] { '/' });
                var builder = new MockResponseBuilder(baseUrl);

                mockServer.Get.Add(
                    builder.GetV3IndexPath(),
                    request =>
                    {
                        return new Action<HttpListenerResponse>(response =>
                        {
                            var mockResponse = builder.BuildV3IndexWithRepoSignResponse(mockServer);
                            response.ContentType = mockResponse.ContentType;
                            MockServer.SetResponseContent(response, mockResponse.Content);
                        });
                    });

                mockServer.Get.Add(
                    builder.GetRepoSignIndexPath(),
                    request =>
                    {
                        return new Action<HttpListenerResponse>(response =>
                        {
                            var mockResponse = builder.BuildRepoSignIndexResponse();
                            response.ContentType = mockResponse.ContentType;
                            MockServer.SetResponseContent(response, mockResponse.Content);
                        });
                    });

                var link = $"{mockServer.SecureUri.TrimEnd(new[] { '/' })}{builder.GetRepoSignIndexPath()}";

                var curr = Process.GetCurrentProcess();

                var id = curr.Id.ToString() + " / " + curr.ProcessName + " / " + curr.ToString();

                var command1 = $"netsh http add sslcert ipport=127.0.0.1:50231 certHash={repoSSLCertificate.Thumbprint} "+ "appid={99d82e36-6b51-4db9-bd37-adbf373125ed}";

                var procStartInfo = new ProcessStartInfo("cmd", "/c " + command1)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var proc = new Process
                {
                    StartInfo = procStartInfo
                };
                proc.Start();
                if (proc.WaitForExit(10000) == false)
                {
                    proc.Kill();
                }

                var allOutput = proc.StandardOutput.ReadToEnd() + Environment.NewLine + proc.StandardError.ReadToEnd();

                mockServer.Start();

                while (true) ;
            }
        }

        private static RepositorySignatureInfo CreateTestRepositorySignatureInfo(List<X509Certificate2> certificates, bool allSigned)
        {
            var repoCertificateInfo = new List<IRepositoryCertificateInfo>();

            foreach (var cert in certificates)
            {
                var certificateFingerprint = CertificateUtility.GetHash(cert, HashAlgorithmName.SHA256);
                var fingerprintString = BitConverter.ToString(certificateFingerprint).Replace("-", "");

                repoCertificateInfo.Add(new TestRepositoryCertificateInfo()
                {
                    ContentUrl = @"https://v3serviceIndex.test/api/index.json",
                    Fingerprints = new Fingerprints(new Dictionary<string, string>()
                    {
                        { HashAlgorithmName.SHA256.ConvertToOidString(), fingerprintString }
                    }),
                    Issuer = cert.Issuer,
                    Subject = cert.Subject,
                    NotBefore = cert.NotBefore,
                    NotAfter = cert.NotAfter
                });
            }

            return new RepositorySignatureInfo(allSigned, repoCertificateInfo);
        }
    }
}
