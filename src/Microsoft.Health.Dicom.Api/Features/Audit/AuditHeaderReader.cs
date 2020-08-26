﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Api.Features.Audit
{
    public class AuditHeaderReader : IAuditHeaderReader
    {
        private readonly AuditConfiguration _auditConfiguration;

        public AuditHeaderReader(IOptions<AuditConfiguration> auditConfiguration)
        {
            EnsureArg.IsNotNull(auditConfiguration?.Value, nameof(auditConfiguration));

            _auditConfiguration = auditConfiguration.Value;
        }

        public IReadOnlyDictionary<string, string> Read(HttpContext httpContext)
        {
            EnsureArg.IsNotNull(httpContext, nameof(httpContext));

            if (httpContext.Items.TryGetValue(AuditConstants.CustomAuditHeaderKeyValue, out object cachedCustomHeaders))
            {
                return cachedCustomHeaders as IReadOnlyDictionary<string, string>;
            }

            var customHeaders = new Dictionary<string, string>();

            foreach (KeyValuePair<string, StringValues> header in httpContext.Request.Headers)
            {
                if (header.Key.StartsWith(_auditConfiguration.CustomAuditHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var headerValue = header.Value.ToString();
                    if (headerValue.Length > AuditConstants.MaximumLengthOfCustomHeader)
                    {
                        throw new AuditHeaderTooLargeException(header.Key, headerValue.Length);
                    }

                    customHeaders[header.Key] = headerValue;
                }
            }

            if (customHeaders.Count > AuditConstants.MaximumNumberOfCustomHeaders)
            {
                throw new AuditHeaderCountExceededException(customHeaders.Count);
            }

            httpContext.Items[AuditConstants.CustomAuditHeaderKeyValue] = customHeaders;
            return customHeaders;
        }
    }
}