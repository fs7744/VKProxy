﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace VKProxy.ACME.AspNetCore;

public class CheckForRenewalAcmeState : AcmeState
{
    private readonly IServerCertificateSource selector;

    public CheckForRenewalAcmeState(IServerCertificateSource selector)
    {
        this.selector = selector;
    }

    public override async Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var checkPeriod = context.Options.RenewalCheckPeriod;
            var daysInAdvance = context.Options.RenewDaysInAdvance;
            if (!checkPeriod.HasValue || !daysInAdvance.HasValue)
            {
                context.Logger.LogInformation("Automatic certificate renewal is not configured. Stopping {service}",
                    nameof(CheckForRenewalAcmeState));
                return null;
            }

            var domainNames = context.Options.DomainNames;
            context.Logger.LogDebug("Checking certificates' renewals for {domainNames}", new StringValues(domainNames));

            foreach (var domainName in domainNames)
            {
                if (!selector.TryGetCertForDomain(domainName, out var cert)
                || cert == null
                    || cert.NotAfter <= DateTimeOffset.Now.DateTime + daysInAdvance.Value)
                {
                    return MoveTo<BeginCertificateCreationAcmeState>();
                }
            }

            await Task.Delay(checkPeriod.Value, stoppingToken);
        }

        return null;
    }
}