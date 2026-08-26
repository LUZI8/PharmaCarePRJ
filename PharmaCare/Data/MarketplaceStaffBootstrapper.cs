namespace PharmaCare.Data;

public static class MarketplaceStaffBootstrapper
{
    public static async Task EnsureAsync(DataDbContext db)
    {
        var mainPharmacy = await db.Pharmacies.OrderBy(x => x.PharmacyId).FirstOrDefaultAsync(x => x.IsActive);
        if (mainPharmacy == null) return;
        var pharmacists = await db.User.AsNoTracking().Where(x => x.IsActive && x.Role == "Pharmacist").Select(x => x.UserId).ToListAsync();
        foreach (var userId in pharmacists)
        {
            if (!await db.PharmacyStaff.AnyAsync(x => x.UserId == userId))
                db.PharmacyStaff.Add(new PharmacyStaff { PharmacyId = mainPharmacy.PharmacyId, UserId = userId, Role = "Pharmacist", IsActive = true });
        }
        await db.SaveChangesAsync();
    }
}
