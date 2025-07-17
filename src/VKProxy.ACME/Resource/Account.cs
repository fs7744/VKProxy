using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace VKProxy.ACME.Resource;

public enum AccountStatus
{
    /// <summary>
    /// The valid status.
    /// </summary>
    Valid,

    /// <summary>
    /// The deactivated status, initiated by client.
    /// </summary>
    Deactivated,

    /// <summary>
    /// The revoked status, initiated by server.
    /// </summary>
    Revoked,
}

public class Account
{
    public AccountStatus? Status { get; set; }

    public List<string>? Contact { get; set; }
    public bool? TermsOfServiceAgreed { get; set; }

    public Uri? Orders { get; set; }

    public object? ExternalAccountBinding { get; set; }

    internal class Payload : Account
    {
        public bool? OnlyReturnExisting { get; set; }
    }
}