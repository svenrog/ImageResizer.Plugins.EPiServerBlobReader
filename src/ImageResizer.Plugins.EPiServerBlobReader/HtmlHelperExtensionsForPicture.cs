﻿using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Core;

namespace ImageResizer.Plugins.EPiServer
{
    public static class HtmlHelperExtensionsForPicture
    {
        public static MvcHtmlString ResizePictures(this HtmlHelper helper, UrlBuilder[] urls, PictureProfile profile, string alternateText = "")
        {
            if(urls == null)
                throw new ArgumentNullException(nameof(urls));
            if(urls.Length == 0)
                throw new ArgumentException($"{nameof(urls)} contains no elements");
            if(profile == null)
                throw new ArgumentNullException(nameof(profile));
            if(profile.SrcMedias == null)
                throw new ArgumentNullException(nameof(profile.SrcMedias));
            if(profile.SrcMedias.Length == 0)
                throw new ArgumentException($"{nameof(profile.SrcMedias)} contains no elements");
            if(urls.Length != profile.SrcMedias?.Length)
                throw new ArgumentException($"Length for `{nameof(urls)}` ({urls.Length}) and `{nameof(profile.SrcMedias)}` ({profile.SrcMedias.Length}) does not match.");

            var picture = new TagBuilder("picture");
            var sources = new StringBuilder();

            for (var i = 0; i < urls.Length; i++)
            {
                var source = new TagBuilder("source");
                var media = profile.SrcMedias[i];
                var width = profile.SrcSetWidths[i];

                var url = urls[i].Clone();
                url.QueryCollection["w"] = width.ToString();

                source.Attributes.Add("srcset", url.ToString());
                source.Attributes.Add("media", media);

                sources.Append(source);
            }

            var img = new TagBuilder("img");
            var defaultUrl = urls.First().Clone();
            defaultUrl.QueryCollection["w"] = profile.DefaultWidth.ToString();
            img.Attributes.Add("src", defaultUrl.ToString());
            img.Attributes.Add("alt", alternateText);

            picture.InnerHtml = sources.ToString() + img;

            return new MvcHtmlString(picture.ToString());
        }

        public static MvcHtmlString ResizePictures(this HtmlHelper helper, ContentReference[] images, PictureProfile profile, string alternateText = "")
        {
            if(images == null)
                throw new ArgumentNullException(nameof(images));

            return helper.ResizePictures(images.Select((c, i) => helper.ResizeImage(c, profile.SrcSetWidths[i])).ToArray(), profile, alternateText);
        }

        public static MvcHtmlString ResizePictureWithFallback(this HtmlHelper helper, ContentReference image, PictureProfile profile, string fallbackImage, string alternateText = "")
        {
            return image == null || image == ContentReference.EmptyReference
                       ? ResizePicture(helper, new UrlBuilder(fallbackImage), profile, alternateText)
                       : ResizePicture(helper, image, profile, alternateText);
        }

        public static MvcHtmlString ResizePicture(this HtmlHelper helper, UrlBuilder url, PictureProfile profile, string alternateText = "")
        {
            var imgUrl = url.Clone();
            imgUrl.QueryCollection["w"] = profile.DefaultWidth.ToString();

            var sourceSets = profile.SrcSetWidths.Select(w =>
                                                         {
                                                             var sourceUrl = url.Clone();
                                                             sourceUrl.QueryCollection["w"] = w.ToString();
                                                             return $"{sourceUrl} {w}w";
                                                         }).ToArray();

            return GeneratePictureElement(profile, imgUrl.ToString(), sourceSets, alternateText);
        }

        public static MvcHtmlString ResizePicture(this HtmlHelper helper, ContentReference image, PictureProfile profile, string alternateText = "")
        {
            var imgUrl = helper.ResizeImage(image, profile.DefaultWidth);
            var sourceSets = profile.SrcSetWidths.Select(w => $"{helper.ResizeImage(image, w).ToString()} {w}w").ToArray();

            return GeneratePictureElement(profile, imgUrl.ToString(), sourceSets, alternateText);
        }

        private static MvcHtmlString GeneratePictureElement(PictureProfile profile, string imgUrl, string[] sourceSets, string alternateText = "")
        {
            var picture = new TagBuilder("picture");
            var source = new TagBuilder("source");

            if(profile.SrcSetSizes != null && profile.SrcSetSizes.Length > 0)
                source.Attributes.Add("sizes", string.Join(", ", profile.SrcSetSizes));

            source.Attributes.Add("srcset", string.Join(", ", sourceSets));

            var img = new TagBuilder("img");
            img.Attributes.Add("src", imgUrl);
            img.Attributes.Add("alt", alternateText);

            picture.InnerHtml = source.ToString() + img;

            return new MvcHtmlString(picture.ToString());
        }
    }
}