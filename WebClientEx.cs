using System;
using System.Net;

namespace MusicBeePlugin
{
    class WebClientEx : WebClient
    {
        public string PreviousReferer { get; private set; }
        public CookieContainer CookieContainer { get; set; }

        public WebClientEx()
            : base()
        {
            CookieContainer = new CookieContainer();
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            if (request is HttpWebRequest req)
            {
                if (PreviousReferer != null)
                {
                    req.Referer = PreviousReferer;
                }
                req.CookieContainer = CookieContainer;
            }
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var response = base.GetWebResponse(request);
            PreviousReferer = response.ResponseUri.AbsoluteUri;

            return response;
        }
    }
}
