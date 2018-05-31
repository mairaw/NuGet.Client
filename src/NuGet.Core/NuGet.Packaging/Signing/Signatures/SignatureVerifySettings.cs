// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Packaging.Signing
{
    /// <summary>
    /// Settings to customize Signature.Verify behavior.
    /// </summary>
    public sealed class SignatureVerifySettings
    {
        /// <summary>
        /// Allow packages with signatures that do not conform to the specification.
        /// </summary>
        public bool AllowIllegal { get; }

        /// <summary>
        /// Specifies that a signing certificate's chain that chains to an untrusted root is allowed
        /// </summary>
        public bool AllowUntrusted { get; }

        /// <summary>
        /// Indicates if untrusted status should be reported.
        /// </summary>
        public bool ReportUntrusted { get; }

        /// <summary>
        /// Specifies that a signing certificate's chain with unknown revocation is allowed.
        /// If set to true, offline revocation is allowed.
        /// </summary>
        public bool AllowUnknownRevocation { get; }

        /// <summary>
        /// Indicates if unknown revocation status should be reported.
        /// </summary>
        public bool ReportUnknownRevocation { get; }

        /// <summary>
        /// Specifies that an error should be logged when the signature is expired.
        /// If set to false, this won't allow expired signatures, only skip the logging of the failure.
        /// </summary>
        public bool LogOnSignatureExpired { get; }

        public SignatureVerifySettings(
            bool allowIllegal,
            bool allowUntrusted,
            bool reportUntrusted,
            bool allowUnknownRevocation,
            bool reportUnknownRevocation,
            bool logOnSignatureExpired)
        {
            AllowIllegal = allowIllegal;
            AllowUntrusted = allowUntrusted;
            ReportUntrusted = reportUntrusted;
            AllowUnknownRevocation = allowUnknownRevocation;
            ReportUnknownRevocation = reportUnknownRevocation;
            LogOnSignatureExpired = logOnSignatureExpired;
        }

        /// <summary>
        /// Get default settings values for relaxed verification on a signature
        /// </summary>
        public static SignatureVerifySettings Default { get; } = new SignatureVerifySettings(
            allowIllegal: false,
            allowUntrusted: true,
            reportUntrusted: true,
            allowUnknownRevocation: true,
            reportUnknownRevocation: true,
            logOnSignatureExpired: true);
    }
}