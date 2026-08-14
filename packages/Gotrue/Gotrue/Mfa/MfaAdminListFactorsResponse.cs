using System.Collections.Generic;

namespace Supabase.Gotrue.Mfa;

public class MfaAdminListFactorsResponse
{
    public List<Factor> Factors { get; set; } = new List<Factor>();
}
