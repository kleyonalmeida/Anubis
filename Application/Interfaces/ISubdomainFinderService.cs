using System.Collections.Generic;
using System.Threading;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface ISubdomainFinderService
{
    /// <summary>
    /// Executes a concurrent search for subdomains utilizing a given wordlist.
    /// Yields results asynchronously as soon as they are resolved and verified.
    /// </summary>
    IAsyncEnumerable<SubdomainResult> FindSubdomainsAsync(
        DomainName targetDomain, 
        IEnumerable<string> wordlist, 
        CancellationToken cancellationToken = default);
}
