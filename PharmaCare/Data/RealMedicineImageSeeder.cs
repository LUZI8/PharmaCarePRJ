using Microsoft.EntityFrameworkCore;
using PharmaCare.Models;

namespace PharmaCare.Data;

/// <summary>
/// Replaces generated demo artwork with real public product/package photography.
/// Remote images keep the demo repository lightweight; locally uploaded admin images are preserved.
/// </summary>
public static class RealMedicineImageSeeder
{
    private static readonly IReadOnlyDictionary<string, string[]> ImagesByProduct =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Panadol Advance 500mg"] = Gallery(
                "https://bf1af2.akinoncloudcdn.com/products/2025/10/14/73253/ec4c6890-39aa-4034-943a-1ef0928b11c5_size3840_cropCenter.jpg",
                "https://www.thefreshmarketdubai.com/cdn/shop/files/6291109120469_d7127921-f76b-4d08-a923-ae9dcbedd7a8.jpg?v=1763097468",
                "https://f.nooncdn.com/p/pzsku/Z092AC0C45E6D161CBC26Z/45/1749646804/3ad37cac-c168-4332-a2a7-82d18fad9f89.jpg?width=800",
                "https://cdn.lifepharmacy.com/products/panadol-advance-tablets-24-s/111396-1.jpg"),

            ["Panadol Extra 500mg"] = Gallery(
                "https://www.meagherspharmacy.ie/cdn/shop/files/panadol-extra-tablets-24-pack-paracetamol-meaghers-pharmacy-14442097082481.jpg?v=1690430412&width=1500",
                "https://liki24.es/image/catalog/product/newupload/427065-133827241760947273.png?h=1000&w=1000",
                "https://cdn.salla.sa/yKErG/Z2S9Q4ioTGIuzAn2srlCH1nleWWvJJjYcgNpYiqf.png",
                "https://lemon.sa/image/cache/catalog/-SH-%20Products/8%20May/04182102%20%20PANADOL%20EXTRA%2024%20TABLETS%20--1400x1400.jpg"),

            ["Panadol ActiFast 500mg"] = Gallery(
                "https://cdn.lifepharmacy.com/products/panadol-actifast-tablets-20-s/111394-1.jpg",
                "https://lemon.sa/image/cache/catalog/pharmacy/products/update-2024/04220222%20Panadol%20Actifast%20500%20mg%20Tab%2020_S%20--1400x1400.jpg",
                "https://www.binsina.ae/media/catalog/product/3/2/32747_1.jpg?bg-color=255%2C255%2C255&canvas=300%3A300&fit=bounds&height=300&optimize=medium&width=300",
                "https://prod-waitrose.azureedge.net/media/cache/07/cf/07cfed512924d1b247bbd6567bc459c5.jpg"),

            ["Nurofen 200mg Tablets"] = Gallery(
                "https://ciplus.bootspharmacyschool.com/dist/img/uploaded/nurofen200mgtabs.jpg",
                "https://cdn11.bigcommerce.com/s-znm0k3lpqn/products/6678/images/8148/4440_nurofen_ibuprofen_200mg_96_tablets_1__62180.1719897484.386.513.jpg?c=1",
                "https://ballsbridgepharmacy.ie/wp-content/uploads/2021/09/NUROFEN-200MG-CAPSULES-1.jpg",
                "https://media.starrymart.co.uk/media/catalog/product/cache/432f1eba02e7fca436a90d18b9c36225/5/0/500167056518.jpg"),

            ["Voltaren Emulgel 1% 50g"] = Gallery(
                "https://cdn.salla.sa/dPKzvr/f09bbb2f-09b7-4cff-9a48-1ad354826e12-1000x1000-QDmEQKrX3yJt4Py2vlqPel2ZKsVgn0IdZiGJ3zsX.png",
                "https://media.zid.store/thumbs/810bcff4-ce05-43e7-b1fa-1894e0ed9fd1/8b1ba014-c476-47d9-8273-3e97c5a8a90f-thumbnail-770x770.png",
                "https://www.mumzworld.com/media/catalog/product/cache/8bf0fdee44d330ce9e3c910273b66bb2/QCOM-36206234-VOLTAREN_9615-1.jpg",
                "https://aptekadlarodziny.pl/media/catalog/product/5/9/5909990173518_2_5588.png?height=930&image-type=image&store=pl&width=930"),

            ["Panadol Cold & Flu Day"] = Gallery(
                "https://alfouadpharmacies.com/cdn/shop/files/Panadol_ColdFlu_Day_24Tabs_PureWhite_1615x1615_68e3dc29-4c17-41d8-99fd-c00df33e1feb.webp?v=1772229613",
                "https://simplificat.com/wp-content/uploads/2023/04/PADIA.jpg"),

            ["Panadol Cold & Flu All in One"] = Gallery(
                "https://static-m2-prod.aaw.com/media/catalog/product/cache/9f18371e3a457e456c922dbc54690d4f/p/h/pharma_catalog_product_p_h_pharma_oct_21_272666-a_92.jpg",
                "https://www.carencurepharmacy.com/cdn/shop/products/60817.jpg?v=1636352931",
                "https://kuludonline.com/cdn/shop/files/9502930972507_3D.jpg?v=1746787246",
                "https://israpharmacy.com/wp-content/uploads/2024/10/Panadol-COLDampFLU-ALL-IN-1-Tablet-24S-Isra-Pharmacy.jpg"),

            ["Strepsils Honey & Lemon"] = Gallery(
                "https://www.healbahrain.com/image/cache/catalog/strepsil/image_2024-07-02_122003528-1400x1400.png",
                "https://bf1af2.akinoncloudcdn.com/products/2025/06/04/73406/d5e8295f-d3e5-4a70-96e0-7c51552785df_size3840_cropCenter.jpg",
                "https://static-01.daraz.com.bd/p/d96a5c4dcf61829516487172b5402ee3.jpg"),

            ["Otrivin Adult Nasal Spray 0.1%"] = Gallery(
                "https://chemcarepharmacy.com/cdn/shop/files/Otrivin_Metered-Dose_10ml.png?v=1714811260&width=990",
                "https://ezypharmacy.co.nz/cdn/shop/products/1_1d8096df-c340-4b4f-a377-d8d1fe1eea71_800x.png?v=1588127019",
                "https://cdn-content-oz1.storbie.com/images/otrivin-adult-nasal-spray-for-blocked-nose-10ml-50.jpg"),

            ["Zyrtec 10mg Tablets"] = Gallery(
                "https://www.apotheka.lt/media/catalog/product/cache/1200_1200/8/c/8caed5d17c6762c073ac2a7cded9d59b.jpg",
                "https://www.manovaistine.lt/private/uploads/images/products/zyrtec-10mg-coat-tab-n10.png",
                "https://www.yliopistonverkkoapteekki.fi/WebRoot/KYA/Shops/KYA/4DBB/8870/F5AE/3D1C/C388/0A28/1051/0027/Zyrtec_10mg_cetirizini_dihydrochloridium_10tablettia_yliopistonverkkoapteekki.jpg"),

            ["Clarityn 10mg Tablets"] = Gallery(
                "https://static.shop-apotheke.at/images/A1253878-p2.jpg"),

            ["Telfast 120mg Tablets"] = Gallery(
                "https://dev.spirit.com.kw/storage/product/1436/123.webp",
                "https://aldawaeya.com/cdn/shop/files/6689182ad458992fa93b1b1b_telfast-120-mg-15-tablet-as-antihistamine.webp?v=1746973909",
                "https://admin.directchemistoutlet.com.au/media/catalog/product/cache/ceccd93bd2605469a252cc5738320185/1/2/12928659128350.jpg",
                "https://wells.pt/dw/image/v2/BFLP_PRD/on/demandware.static/-/Sites-wells-master-catalog/default/dwa5f43b1d/images/wells/690/6903711-TELFAST-120-COMPRIMIDOS-ANTI-ALERGICO-TELFAST-P-01.jpg?sh=387&sm=fit&sw=387"),

            ["Gaviscon Double Action 300ml"] = Gallery(
                "https://welzo.com/cdn/shop/files/gaviscon-double-action-liquid-welzo-5_1445x.jpg?v=1698948509"),

            ["Rennie Peppermint Tablets"] = Gallery(
                "https://www.qbicwashrooms.co.uk/media/catalog/product/r/e/rennie_peppermint.jpg",
                "https://images-na.ssl-images-amazon.com/images/I/51lmYwA%2BxfL._SS400_.jpg"),

            ["Dulcolax 5mg Tablets"] = Gallery(
                "https://tiimg.tistatic.com/fp/1/007/781/dulcolax-bisacodyl-tablets--558.jpg"),

            ["Centrum Adults Multivitamin"] = Gallery(
                "https://cd3c14.cdn.akinoncloud.com/products/2025/10/05/59374/d07a14fd-51c0-4a64-9b7e-238b474f1244_size3840x3840_cropCenter.jpg"),

            ["Vitamin D3 1000 IU"] = Gallery(
                "https://dashboard.800pharmacy.ae/image/catalog/Products/117044.jpg",
                "https://digital.loblaws.ca/SDM/SDM_057800840138/en/1/57800840138_en_01_1200.jpeg",
                "https://freedahealth.com/cdn/shop/files/freeda-vitamin-d3-1000-iu-tiny-tablets-1012037335.png?v=1768051510",
                "https://m.media-amazon.com/images/I/71wlaWQy%2BYL._SL1500_.jpg"),

            ["Vitamin C 1000mg Effervescent"] = Gallery(
                "https://www.truthcarepharmacy.com/cdn/shop/files/13189.jpg?v=1715413787",
                "https://www.truthcarepharmacy.com/cdn/shop/files/13189_229fe11a-4281-4f1f-9abf-6420657198f7.jpg?v=1745579345&width=1946",
                "https://kuludonline.com/cdn/shop/files/41555.jpg?v=1746715762",
                "https://api.pharmaplus.co.ke/images/Supplements/vit%20c%20eff%20orange.png"),

            ["Bepanthen Cream 30g"] = Gallery(
                "https://ropharma.ro/image/cache/img/jpg/catalog/nom_products/ropharma/bepanthen-5--crema-30g-1024x1024.webp",
                "https://onlinepatikamm.cdn.shoprenter.hu/custom/onlinepatikamm/image/data/uploads/2019/02/apivita-1.png.webp?lastmod=1708288961.1693165075",
                "https://images-cdn.ubuy.com.bo/68fb3663a56d45a4f70282b7-bepanthen-cream-30-g-bayer.jpg"),

            ["Sudocrem Antiseptic Cream 125g"] = Gallery(
                "https://rokbucket.rokomari.io/ProductNew20190903/260X372/Sudocrem_Antiseptic_Healing_Cream_125_gm-Sudocrem-7ca1a-389099.png",
                "https://m.yuehlia.com/wp-content/uploads/2020/07/01140959/Sudo-Cream-Antiseptic-Healing-Cream-125g.jpg",
                "https://elegantsmockers.lk/cdn/shop/files/Sudocrem-Elegant-Smockers_1024x1024.jpg?v=1692340751",
                "https://mehnurbabyshop.com/cdn/shop/files/SudocreamAntisepticHealingCream125gp1.jpg?v=1692707068&width=1445"),

            ["Savlon Antiseptic Cream 30g"] = Gallery(
                "https://nationalpharmacies.lbcdn.io/app/uploads/2019/05/RBHE_9300711023901-7.jpg",
                "https://www.tastefuldelights.com.au/434919-medium_default/tube-savlon-30g.jpg",
                "https://cdn.shopify.com/s/files/1/1253/1339/products/10103005_2_1200x1200.jpg?v=1481518209",
                "https://safeworx.co.nz/Images/ProductImages/SAVLON.jpg"),

            ["Amoxicillin 500mg Capsules"] = Gallery(
                "https://assetpharmacy.com/wp-content/uploads/2024/01/Amoxicillin-Capsules-10-Capsules-1200x900-cropped.jpg",
                "https://www.ddgroup.com/globalassets/productimages/naa019/naa019_1.jpg",
                "https://assetpharmacy.com/wp-content/uploads/2024/01/Amoxicillin-Capsules-10-Capsules.jpg",
                "https://survival-32.azurewebsites.net/images/thumbs/0004727_amoxicillin-500mg-capsules-21.jpeg"),

            ["Augmentin 625mg Tablets"] = Gallery(
                "https://d1t78adged64l7.cloudfront.net/images/medicines/1677653705_VruQi8QX11.webp"),

            ["Azithromycin 500mg Tablets"] = Gallery(
                "https://res.cloudinary.com/zava-www-uk/image/upload/fl_progressive/a_exif%2Cf_auto%2Ce_sharpen%3A100%2Cc_fit%2Cw_1080%2Ch_810%2Cq_70/v1701187775/sd/uk/services-setup/travellers-diarrhoea-unit/azithromycin/tbzxjezmuvdkx5y3owya.jpg",
                "https://www.transpharm.co.za/medias/515Wx515H-59765-01.jpg?context=bWFzdGVyfGltYWdlc3wxMDUzOTJ8aW1hZ2UvanBlZ3xhRFpsTDJobVlTOHhNakl3TVRNeU1qUXhOREV4TUM4MU1UVlhlRFV4TlVoZk5UazNOalZmTURFdWFuQm58NDU2ZGE5ZTlmMTUyMDZjNDkwNjA3YWE3OWQxNjM4ZGMyNWIxNTNhMDlhOWJmY2QyMzVhOTc3MTAyOTViN2I5Mg"),

            ["Montelukast 10mg Tablets"] = Gallery(
                "https://vonagepharma.com/wp-content/uploads/2023/11/Montelukast-10-Front.jpg",
                "https://vonagepharma.com/wp-content/uploads/2023/11/Montelukast-10-leftside.jpg",
                "https://vonagepharma.com/wp-content/uploads/2023/11/Montelukast-10-Rightside.jpg"),

            ["Salbutamol Inhaler 100mcg"] = Gallery(
                "https://derma.pk/cdn/shop/files/Ventolin_Evohaler_100mcg_200_Actuations.webp?v=1752866512&width=720",
                "https://www.cutpricepharmacy.com.au/cdn/shop/files/f_2d_b5cc1482-3d2b-4173-b7ad-a76d68ca610a_grande.jpg?v=1715808129",
                "https://www.efarma.nl/itempics/p/zp_14127202.jpg",
                "https://static.wixstatic.com/media/bfa046_1bfabf76ad2e43e180a5e570486f49e4~mv2.jpg/v1/fill/w_2006%2Ch_2007%2Cal_c%2Cq_90%2Cenc_avif%2Cquality_auto/bfa046_1bfabf76ad2e43e180a5e570486f49e4~mv2.jpg"),

            ["Budesonide/Formoterol Inhaler"] = Gallery(
                "https://mmassets.universaldrugstore.com/wp-content/uploads/product-image-symbicort-inhaler-1052x1052.webp",
                "https://cd3c14-whites.akinoncloudcdn.com/products/2025/09/09/31514/19bdd214-2d8e-445d-82e4-b46287f5dd74_size640x640_cropCenter.jpg",
                "https://www2cdn.web.health.state.mn.us/diseases/asthma/medications/images/enlarged/Symbicort-160.png",
                "https://www.alpropharmacy.com/cdn/shop/files/00008723_L_4.jpg?v=1759236792&width=1445"),

            ["Metformin 500mg Tablets"] = Gallery(
                "https://shoprite-ecommerce-prod-cdn.azureedge.net/sys-master-images/he4/h93/12321767948318/515Wx515H_107814_01.jpg",
                "https://shoprite-ecommerce-prod-cdn.azureedge.net/sys-master-images/hf2/haf/9479627538462/515Wx515H_57272_01.jpg",
                "https://assetpharmacy.com/wp-content/uploads/2017/09/Metformin-500mg-Tablets-100-Tablets.jpg",
                "https://online-pharmacy4u.co.uk/cdn/shop/files/Metformin_Tablet_s_2_800x.webp?v=1730728815"),

            ["Gliclazide MR 30mg Tablets"] = Gallery(
                "https://media.myaster.com/images/products/1044270/g-zide-mr-30mg-tablets-pack-of-28s/1044270-1neww.jpg",
                "https://online-apteka.com.ua/assets/images/products/88974/dyahlyzyd-mr-tabl-30mh-30-0.jpg"),

            ["Sitagliptin 100mg Tablets"] = Gallery(
                "https://frankrosspharmacy.com/_next/image?q=75&url=https%3A%2F%2Femami-production-2.s3.amazonaws.com%2Fvariant_images%2Ffiles%2F000%2F038%2F274%2Fnormal_webp%2FFR-44893.webp%3F1686224414&w=640",
                "https://cdn.pixelbin.io/v2/plain-cake-860195/netmed/wrkr/products/assets/item/free/original/jAIKw3PnR5-stig_100_tablet_15s_453531_0_0.jpg",
                "https://kuludonline.com/cdn/shop/files/67021_c33e3d70-705e-46c7-9239-3bd6fcb2aa33.jpg?v=1746793418",
                "https://cdn.salla.sa/alOaz/3864a83f-6abd-45b2-97d4-3bfce5989f99-1000x1000-t497j3NqRQrKoV8PoWkynQQ5PJbgyhVKPJFoFYYx.jpg"),

            ["Amlodipine 5mg Tablets"] = Gallery(
                "https://assetpharmacy.com/wp-content/uploads/2017/09/Amlodipine-5mg-Tablets-28-Tablets-1.jpg",
                "https://www.swifthealmedical.com/storage/14.jpg",
                "https://www.assetpharmacy.com/wp-content/uploads/2017/09/Amlodipine-5mg-Tablets-28-Tablets-1-1200x630-cropped.jpg",
                "https://doctormpharmacy.com/cdn/shop/files/49444.jpg?v=1755364050&width=533"),

            ["Losartan 50mg Tablets"] = Gallery(
                "https://www.transpharm.co.za/medias/515Wx515H-92025-01.jpg?context=bWFzdGVyfGltYWdlc3wxMjgzODB8aW1hZ2UvanBlZ3xhREl6TDJoa055ODVORGcwTWpBMU1EWTBNakl5THpVeE5WZDROVEUxU0Y4NU1qQXlOVjh3TVM1cWNHY3xmZGQ0NjIxZDFlZGI4M2YxOGU3MWM4ZDRmZThiYWJiMzU4NDY3MzIyYWY2NzQyZjAxMGYwNWMzMjE4MmZmODFm",
                "https://www.meldinpharma.com/web/image/product.product/1062/image_1920?unique=08be22b",
                "https://costofarma.mx/cdn/shop/files/821998000229a.jpg?v=1756269493",
                "https://salomat.tj/upload_product/79_Lazortan__salomat.jpg"),

            ["Bisoprolol 5mg Tablets"] = Gallery(
                "https://www.add.ua/media/catalog/product/cache/207e23213cf636ccdef205098cf3c8a3/b/i/bisoprol-5-mg-_50-1.jpg",
                "https://ukrstore.com/media/catalog/product/cache/1/image/600x600/9df78eab33525d08d6e5fb8d27136e95/b/i/bisoprol20_tablets_2_5mg_bisoprolol_bisoprololum_-__2.png",
                "https://storage.googleapis.com/static-storage/products/images/v2/resized/test10/bisoprol-tabletki-5-mg-blister-30-1048702.jpg",
                "https://cdn.27.ua/799/88/a0/3246240_1.jpeg"),

            ["Atorvastatin 20mg Tablets"] = Gallery(
                "https://healthplusnigeria.com/cdn/shop/files/Avstat_20-_2020_20Atorvastatin_2020mg_20x30_bd96da21-eabf-4b04-a69c-fa23fdb00d1d.webp?v=1764103155",
                "https://avenzor.net/img/uploads1/prod_photo20151220094716.jpg",
                "https://countrymedicalpharmacy.com/wp-content/uploads/2023/07/EixdtuFWkAAZlGg.jpg",
                "https://frankrosspharmacy.com/_next/image?q=75&url=https%3A%2F%2Femami-production-2.s3.amazonaws.com%2Fvariant_images%2Ffiles%2F000%2F021%2F091%2Fnormal_webp%2FDSC_0501.webp%3F1650024321&w=640"),

            ["Rosuvastatin 10mg Tablets"] = Gallery(
                "https://www.carelink.lk/wp-content/uploads/2022/11/Rusat_10mg_Carelink.jpg",
                "https://tdawi.com/media/catalog/product/cache/72f8467a5043ecc9af047df47b9246a4/r/o/rosuvastatin_10_mg_3__t7hxmym8hosvuctk.jpg",
                "https://4yfweck668yj.b-cdn.net/3ngemuwaq0bo/rosuvastatin-2-photoroom.png",
                "https://www.mycare.lk/image/cache/catalog/products/010635-550x550.jpg")
        };

    public static async Task SeedAsync(DataDbContext db, ILogger logger)
    {
        var products = await db.Product
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .ToListAsync();

        var updated = 0;

        foreach (var product in products)
        {
            if (!ImagesByProduct.TryGetValue(product.ProductName, out var gallery) || gallery.Length == 0)
                continue;

            // Keep an image gallery that the admin uploaded to this project. Remote/generated demo
            // URLs are safe to refresh from the catalog below.
            if (IsLocalAdminImage(product.ImageUrl))
                continue;

            var currentGallery = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .ToArray();

            if (string.Equals(product.ImageUrl, gallery[0], StringComparison.OrdinalIgnoreCase) &&
                currentGallery.SequenceEqual(gallery, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (product.Images.Count > 0)
            {
                db.ProductImages.RemoveRange(product.Images);
                product.Images.Clear();
            }

            product.ImageUrl = gallery[0];
            product.UpdatedAt = DateTime.UtcNow;

            for (var index = 0; index < gallery.Length; index++)
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = gallery[index],
                    DisplayOrder = index,
                    IsPrimary = index == 0,
                    CreatedAt = DateTime.UtcNow.AddSeconds(index)
                });
            }

            updated++;
        }

        if (updated > 0)
            await db.SaveChangesAsync();

        logger.LogInformation("Applied real package photography to {Count} active catalog products.", updated);
    }

    private static bool IsLocalAdminImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;

        return imageUrl.StartsWith("/", StringComparison.Ordinal) ||
               imageUrl.StartsWith("~/", StringComparison.Ordinal) ||
               imageUrl.StartsWith("images/", StringComparison.OrdinalIgnoreCase) ||
               imageUrl.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] Gallery(params string[] urls)
    {
        var result = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();

        if (result.Count == 0)
            return Array.Empty<string>();

        // Existing storefront gallery is designed around four image slots. If only one or two
        // stable public packshots exist, keep all four slots real instead of falling back to artwork.
        while (result.Count < 4)
            result.Add(result[^1]);

        return result.ToArray();
    }
}
