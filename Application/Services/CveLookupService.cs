using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class CveLookupService : ICveLookupService
{
    private readonly INvdApiClient _nvdApiClient;
    private readonly ILogger<CveLookupService> _logger;

    public CveLookupService(INvdApiClient nvdApiClient, ILogger<CveLookupService> logger)
    {
        _nvdApiClient = nvdApiClient;
        _logger = logger;
    }

    public Task<IReadOnlyList<CveResult>> SearchByProductAsync(string productKeyword, CancellationToken cancellationToken = default)
    {
        return _nvdApiClient.GetVulnerabilitiesByKeywordAsync(productKeyword, cancellationToken);
    }

    public Task<CveResult?> SearchByCveIdAsync(string cveId, CancellationToken cancellationToken = default)
    {
        return _nvdApiClient.GetVulnerabilityByIdAsync(cveId, cancellationToken);
    }
}
