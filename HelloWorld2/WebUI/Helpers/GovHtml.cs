using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Collections;
using System.IO;
using System.Text.Encodings.Web;

namespace uk.gov {


    public static class Gov {
        public static IHtmlContent h1(string text) { return h1(new HtmlString(text)); }
        public static IHtmlContent h1(Func<IHtmlContent, IHtmlContent> text) {return h1(text(new HtmlString("")));}

        public static IHtmlContent h1(IHtmlContent htmlString)
        {
            return new HtmlString(String.Format(@"<h1 class=""heading-xlarge"">{0}</h1>", GetContent(htmlString)));
        }

        public static IHtmlContent leadPara(string text) { return leadPara(new HtmlString(text)); }
        public static IHtmlContent leadPara(Func<IHtmlContent, IHtmlContent> text) {return leadPara(text(new HtmlString("")));}

        public static IHtmlContent leadPara(IHtmlContent htmlString)
        {
            return new HtmlString(String.Format(@"<p class=""lede"">{0}</p>", GetContent(htmlString)));
        }

        public static IHtmlContent list(IEnumerable list, Func<dynamic, IHtmlContent> entryTemplate) {
            return new HtmlString(String.Format(@"<ul class=""list"">{0}</ul>", String.Join(" ", ((IEnumerable<dynamic>)list).Select(entryTemplate).Select(x => String.Format("<li>{0}</li>", GetContent(x))))));
        }

        private static string GetContent(IHtmlContent content) {
            var sw = new StringWriter();
            content.WriteTo(sw, HtmlEncoder.Default);
            return sw.ToString();
        }

    }

    public static class GovTemplates {

        public static Func<Func<IHtmlContent, object>, IHtmlContent> oneArgTemplate(Func<IHtmlContent, Func<IHtmlContent, IHtmlContent>> template) {
            return (arg) => {
                var argRendered = evalAsHtmlContent(arg);
                var tmp = template(argRendered);
                return evalAsHtmlContent(tmp);
            };
        }

        public static Func<Func<IHtmlContent, object>, Func<IHtmlContent, object>, IHtmlContent> twoArgTemplate(Func<IHtmlContent, IHtmlContent, Func<IHtmlContent, IHtmlContent>> template) {
            return (arg1, arg2) => {
                var arg1rendered = evalAsHtmlContent(arg1);
                var arg2rendered = evalAsHtmlContent(arg2);
                var tmp = template(arg1rendered, arg2rendered);
                return evalAsHtmlContent(tmp);
            };
        }

        public static Func<IList<Func<IHtmlContent, object>>, IHtmlContent> multiArgTemplate(Func<IList<IHtmlContent>, Func<IHtmlContent,IHtmlContent>> template) {
            return args => {
                var rendered = args.Select(evalAsHtmlContent).ToList();
                var tmp = template(rendered);
                return evalAsHtmlContent(tmp);
            };
        }

        private static IHtmlContent evalAsHtmlContent(Func<IHtmlContent, object> theFunc) {
                object x = theFunc(new HtmlString(""));
                if (x is IHtmlContent) return (IHtmlContent) x;
                else return new HtmlString(x.ToString());
            
        }
    }
    public static class GovHtmlHelper {

        public static GovHtml Gov(this IHtmlHelper html) {
            return new GovHtml(html);
            
        }
    }

    public class GovHtml {
        private IHtmlHelper html;

        public GovHtml(IHtmlHelper html)
        {
            this.html = html;
        }

        public IDisposable GridRow() {
            return new EncapsulatingDisposable(html,
                @"<div class=""container-row"">",
                @"</div>");            
        }

        public IDisposable TwoThirds() {
            return new EncapsulatingDisposable(html,
                @"<div class=""column-two-thirds"">",
                @"</div>");
        }

        public IDisposable OneThird() {
            return new EncapsulatingDisposable(html,
                @"<div class=""column-one-third"">",
                @"</div>");
        }
        

        private class EncapsulatingDisposable : IDisposable
        {
            private readonly IHtmlHelper html;
            private readonly string closer;

            public EncapsulatingDisposable(IHtmlHelper html, String opener, String closer)
            {
                html.ViewContext.Writer.Write(opener); 
                this.closer = closer;
                this.html = html;
            }

            public void Dispose()
            {
                html.ViewContext.Writer.Write(closer);
            }
        }
    }
}