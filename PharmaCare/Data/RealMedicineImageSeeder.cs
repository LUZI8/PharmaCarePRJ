using Microsoft.EntityFrameworkCore;
using PharmaCare.Models;

namespace PharmaCare.Data;

/// <summary>
/// Replaces demo placeholder artwork with real medicine/package photography for products where
/// a verified product-specific image source is available. Admin-uploaded/custom galleries are
/// preserved because only products still using the demo placehold.co artwork are refreshed.
/// </summary>
public static class RealMedicineImageSeeder
{
    private static readonly Dictionary<string, string[]> Images = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Panadol Advance 500mg"] = new[]
        {
            "https://springs.com.pk/cdn/shop/files/5000347029462.gif?v=1747834681",
            "https://www.medicalmartpk.com/cdn/shop/products/PanadolParacetamolTabletsPainRelief500mgAdvance16s_800x.jpg?v=1617889435",
            "https://mercurydeliver.com/cdn/shop/files/BC_Upload_e6568db6-3f7c-49e4-a5b3-9b3112851c95.jpg?v=1715608560",
            "https://lloydspharmacy.com/cdn/shop/files/05054563125491_T1.jpg?v=1767369066"
        },
        ["Panadol ActiFast 500mg"] = Repeat("https://digitalcontent.api.tesco.com/v2/media/ghs/8aed105e-b560-498e-b576-fadca0444f63/446aa86e-89f8-42f4-8be7-ac860f716676_457189772.jpeg"),
        ["Nurofen 200mg Tablets"] = new[]
        {
            "https://ciplus.bootspharmacyschool.com/dist/img/uploaded/nurofen200mgtabs.jpg",
            "https://cdn11.bigcommerce.com/s-znm0k3lpqn/products/6678/images/8148/4440_nurofen_ibuprofen_200mg_96_tablets_1__62180.1719897484.386.513.jpg?c=1",
            "https://bigamartusax.s3-accelerate.amazonaws.com/2020/05/61To7ZwQqUL._AC_SL1000_.jpg",
            "https://www.bargainchemist.co.nz/cdn/shop/products/10169895354398_1024x.jpg?v=1615235688"
        },
        ["Voltaren Emulgel 1% 50g"] = new[]
        {
            "https://cdn.salla.sa/dPKzvr/f09bbb2f-09b7-4cff-9a48-1ad354826e12-1000x1000-QDmEQKrX3yJt4Py2vlqPel2ZKsVgn0IdZiGJ3zsX.png",
            "https://www.ehavene.com.bd/uploads/products/photos/GnM9oJlon8TD8DhatpvhXqv5EXMsZwPUkFxiRn7t.jpg",
            "https://emedika.bg/image/data/Others/voltaren-emulgel.png",
            "https://admin.viafarm.mk/uploads/items/4313/1654862323voltaren-emulgel-1-gel-za-bolki-50g-1-1000x520-pad.jpg"
        },
        ["Strepsils Honey & Lemon"] = new[]
        {
            "https://p1-cdn.myaster.com/1aster-prod-media/media/catalog/product/1/0/1010435_1new.jpg",
            "https://121pharmacy.co.uk/cdn/shop/files/5000158100527_0_1024x1024.jpg?v=1737470894",
            "https://ro.britishessentials.com/cdn/shop/files/May282314_x700.jpg?v=1685240520",
            "https://medino-product.imgix.net/strepsils-honey-and-lemon-16-lozenges--1774727836.png?auto=format%2Ccompress&bg=FFF&h=609&q=60"
        },
        ["Otrivin Adult Nasal Spray 0.1%"] = Repeat("https://media.myaster.com/images/products/1008458/otrivin-nasal-spray-0-1-10-ml/1008458_6.jpg?fit=bounds&width=840"),
        ["Telfast 120mg Tablets"] = new[]
        {
            "https://dev.spirit.com.kw/storage/product/1436/123.webp",
            "https://admin.directchemistoutlet.com.au/media/catalog/product/cache/ceccd93bd2605469a252cc5738320185/1/2/12928659128350.jpg",
            "https://cdn.lfafirstresponse.com.au/product-files/593613/featured.png",
            "https://www.directchemistoutlet.com.au/cdn/shop/files/12928664567838.jpg?v=1778518819"
        },
        ["Gaviscon Double Action 300ml"] = new[]
        {
            "https://m2.alhabibpharmacy.net/media/catalog/product/N/e/New_image_6340160239_0.jpg",
            "https://unitedpharmacy.sa/media/catalog/product/cache/0cf3b82d6478eac521dcd4529e64ed76/g/a/gaviscon_double_action_300ml_susp._1_.png",
            "https://welzo.com/cdn/shop/files/gaviscon-double-action-liquid-welzo-5_1445x.jpg?v=1698948509",
            "https://m2.alhabibpharmacy.net/media/catalog/product/N/e/New_image_6340160239_0.jpg"
        },
        ["Rennie Peppermint Tablets"] = Repeat("https://m2.ukmeds.co.uk/media/catalog/product/cache/74c1057f7991b4edb2bc7bdaa94de933/r/e/rennie_peppermint_72_tablets-3.jpg"),
        ["Dulcolax 5mg Tablets"] = new[]
        {
            "https://media.zid.store/af67ae11-520e-4132-a131-39a3e5789dac/41d2010f-29db-445a-85d7-76e14010fdf7.jpeg",
            "https://tiimg.tistatic.com/fp/1/007/781/dulcolax-bisacodyl-tablets--558.jpg",
            "https://cpimg.tistatic.com/03696392/b/6/Bisacodyl-Enteric-Coated-Tablets.jpg",
            "https://sunwaymulticare.com.my/cdn/shop/files/dulcolax.jpg?v=1739438981"
        },
        ["Centrum Adults Multivitamin"] = Repeat("https://i5.walmartimages.com/asr/450fde32-5704-45f4-a0e0-828c169ee8fc.af9076b3ec98703af9d3f69008aef764.jpeg?odnBg=FFFFFF&odnHeight=768&odnWidth=768"),
        ["Sudocrem Antiseptic Cream 125g"] = new[]
        {
            "https://rokbucket.rokomari.io/ProductNew20190903/260X372/Sudocrem_Antiseptic_Healing_Cream_125_gm-Sudocrem-7ca1a-389099.png",
            "https://m.yuehlia.com/wp-content/uploads/2020/07/01140959/Sudo-Cream-Antiseptic-Healing-Cream-125g.jpg",
            "https://elegantsmockers.lk/cdn/shop/files/Sudocrem-Elegant-Smockers_1024x1024.jpg?v=1692340751",
            "https://rokbucket.rokomari.io/ProductNew20190903/260X372/Sudocrem_Antiseptic_Healing_Cream_125_gm-Sudocrem-7ca1a-389099.png"
        },
        ["Savlon Antiseptic Cream 30g"] = Repeat("https://www.aci-bd.com/assets/images/products/antiseptic/cream/savlon-antiseptic-cream-30g.jpg"),
        ["Amoxicillin 500mg Capsules"] = new[]
        {
            "https://assetpharmacy.com/wp-content/uploads/2024/01/Amoxicillin-Capsules-10-Capsules-1200x900-cropped.jpg",
            "https://assetpharmacy.com/wp-content/uploads/2024/01/Amoxicillin-Capsules-10-Capsules.jpg",
            "https://assetpharmacy.com/wp-content/uploads/2024/01/Amoxicillin-Capsules-10-Capsules-1200x900-cropped.jpg",
            "https://assetpharmacy.com/wp-content/uploads/2024/01/Amoxicillin-Capsules-10-Capsules.jpg"
        },
        ["Augmentin 625mg Tablets"] = new[]
        {
            "https://dawahealthcare.biz/cdn/shop/files/augmentin-625mg.webp?v=1704153142",
            "https://meripharmacy.pk/cdn/shop/products/625Augmentan_13ee3137-fc8f-4593-8ba9-fa639237e322_700x700.jpg?v=1752791048",
            "https://www.dsmonline.pk/media/catalog/product/cache/e626209f6586797a49e0d0a395e17e33/f/q/fq24f3.png",
            "https://dawahealthcare.biz/cdn/shop/files/augmentin-625mg.webp?v=1704153142"
        },
        ["Gliclazide MR 30mg Tablets"] = new[]
        {
            "https://globelapharma.com/wp-content/uploads/2023/01/GLICLAZIDE-MR_.png",
            "https://globelapharma.com/wp-content/uploads/2023/01/GLICLAZIDE-MR_-600x600.png",
            "https://www.shopaholic.pk/cdn/shop/files/1012242-1.jpg?v=1744241299",
            "https://img1.exportersindia.com/product_images/bc-full/2022/10/7120933/gliclazide-30-mg-1665570577-6581301.jpeg"
        },
        ["Losartan 50mg Tablets"] = new[]
        {
            "https://www.transpharm.co.za/medias/515Wx515H-92025-01.jpg?context=bWFzdGVyfGltYWdlc3wxMjgzODB8aW1hZ2UvanBlZ3xhREl6TDJoa055ODVORGcwTWpBMU1EWTBNakl5THpVeE5WZDROVEUxU0Y4NU1qQXlOVjh3TVM1cWNHY3xmZGQ0NjIxZDFlZGI4M2YxOGU3MWM4ZDRmZThiYWJiMzU4NDY3MzIyYWY2NzQyZjAxMGYwNWMzMjE4MmZmODFm",
            "https://farmaciauniversalpe.vtexassets.com/arquivos/ids/162800/23827_1.jpg?v=638848347202500000",
            "https://costofarma.mx/cdn/shop/files/821998000229a.jpg?v=1756269493",
            "https://ecommerce.genericartmedicine.com/images/products/product-photo-14418032024150304.jpg"
        },
        ["Bisoprolol 5mg Tablets"] = Repeat("https://www.add.ua/media/catalog/product/cache/207e23213cf636ccdef205098cf3c8a3/b/i/bisoprol-5-mg-_50-1.jpg")
    };

    public static async Task SeedAsync(DataDbContext db, ILogger logger)
    {
        var products = await db.Product.Include(p => p.Images).ToListAsync();
        var updated = 0;

        foreach (var product in products)
        {
            if (!Images.TryGetValue(product.ProductName, out var gallery))
                continue;

            // Never overwrite images the admin has already uploaded/changed manually.
            if (!string.IsNullOrWhiteSpace(product.ImageUrl) && !product.ImageUrl.Contains("placehold.co", StringComparison.OrdinalIgnoreCase))
                continue;

            if (product.Images.Count > 0)
            {
                db.ProductImages.RemoveRange(product.Images);
                product.Images.Clear();
            }

            product.ImageUrl = gallery[0];
            for (var i = 0; i < gallery.Length; i++)
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = gallery[i],
                    DisplayOrder = i,
                    IsPrimary = i == 0,
                    CreatedAt = DateTime.UtcNow.AddSeconds(i)
                });
            }

            product.UpdatedAt = DateTime.UtcNow;
            updated++;
        }

        if (updated > 0)
            await db.SaveChangesAsync();

        logger.LogInformation("Applied real product photography to {Count} catalog products.", updated);
    }

    private static string[] Repeat(string url) => new[] { url, url, url, url };
}
