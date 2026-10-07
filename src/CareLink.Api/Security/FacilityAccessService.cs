using System.Security.Claims;

namespace CareLink.Api.Security;

public interface IFacilityAccessService
{
    bool CanAccess(ClaimsPrincipal user, string facilityId);
}

public sealed class FacilityAccessService : IFacilityAccessService
{
    public bool CanAccess(ClaimsPrincipal user, string facilityId)
    {
        if (user.IsInRole(CareLinkRoles.Manager))
        {
            return true;
        }

        return user.IsInRole(CareLinkRoles.ClinicStaff) &&
               string.Equals(
                   user.FindFirstValue("facility_id"),
                   facilityId,
                   StringComparison.OrdinalIgnoreCase);
    }
}
