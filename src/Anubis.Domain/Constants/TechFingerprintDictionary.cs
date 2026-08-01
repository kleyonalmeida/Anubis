using System.Collections.Generic;
using Anubis.Domain.ValueObjects;

namespace Anubis.Domain.Constants;

public static class TechFingerprintDictionary
{
    // Mapeamento 1:1 do Python para o nosso ValueObject
    public static readonly IReadOnlyDictionary<string, TechSignature[]> Signatures = new Dictionary<string, TechSignature[]>
    {
        { "WordPress", new[] { new TechSignature(TechSignatureType.Html, "wp-content"), new TechSignature(TechSignatureType.Html, "wp-includes") } },
        { "Joomla", new[] { new TechSignature(TechSignatureType.Html, "joomla"), new TechSignature(TechSignatureType.Html, "/components/com_") } },
        { "Drupal", new[] { new TechSignature(TechSignatureType.Html, "drupal"), new TechSignature(TechSignatureType.Cookie, "drupal") } },
        { "Magento", new[] { new TechSignature(TechSignatureType.Html, "magento"), new TechSignature(TechSignatureType.Cookie, "frontend") } },
        { "Shopify", new[] { new TechSignature(TechSignatureType.Html, "shopify"), new TechSignature(TechSignatureType.Html, "cdn.shopify.com") } },
        { "React", new[] { new TechSignature(TechSignatureType.Html, "__react"), new TechSignature(TechSignatureType.Html, "react.production") } },
        { "Vue.js", new[] { new TechSignature(TechSignatureType.Html, "vue.js"), new TechSignature(TechSignatureType.Html, "__vue__") } },
        { "Angular", new[] { new TechSignature(TechSignatureType.Html, "ng-version"), new TechSignature(TechSignatureType.Html, "ng-app") } },
        { "Next.js", new[] { new TechSignature(TechSignatureType.Html, "_next/static") } },
        { "jQuery", new[] { new TechSignature(TechSignatureType.Html, "jquery.min.js"), new TechSignature(TechSignatureType.Html, "jquery.js") } },
        { "Bootstrap", new[] { new TechSignature(TechSignatureType.Html, "bootstrap.min.css"), new TechSignature(TechSignatureType.Html, "bootstrap.min.js") } },
        { "Tailwind", new[] { new TechSignature(TechSignatureType.Html, "tailwindcss"), new TechSignature(TechSignatureType.Html, "tailwind") } },
        
        { "Apache", new[] { new TechSignature(TechSignatureType.HeaderServer, "apache") } },
        { "Nginx", new[] { new TechSignature(TechSignatureType.HeaderServer, "nginx") } },
        { "IIS", new[] { new TechSignature(TechSignatureType.HeaderServer, "iis") } },
        
        { "Cloudflare", new[] { new TechSignature(TechSignatureType.Header, "cf-ray") } },
        { "AWS CloudFront", new[] { new TechSignature(TechSignatureType.Header, "x-amz-cf-id") } },
        { "Vercel", new[] { new TechSignature(TechSignatureType.Header, "x-vercel-id") } },
        
        { "PHP", new[] { new TechSignature(TechSignatureType.HeaderXPoweredBy, "php"), new TechSignature(TechSignatureType.Html, ".php") } },
        { "ASP.NET", new[] { new TechSignature(TechSignatureType.HeaderXPoweredBy, "asp.net") } },
        { "Python/Django", new[] { new TechSignature(TechSignatureType.Cookie, "csrftoken"), new TechSignature(TechSignatureType.Cookie, "sessionid") } },
        { "Node.js/Express", new[] { new TechSignature(TechSignatureType.HeaderXPoweredBy, "express") } },
        
        { "Google Analytics", new[] { new TechSignature(TechSignatureType.Html, "google-analytics.com"), new TechSignature(TechSignatureType.Html, "gtag(") } },
        { "Google Tag Manager", new[] { new TechSignature(TechSignatureType.Html, "googletagmanager.com") } }
    };
}
