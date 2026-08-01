using System.Collections.Generic;
using System.Threading;
using Anubis.Domain.Entities;
using Anubis.Domain.ValueObjects;

namespace Anubis.Application.Interfaces;

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
