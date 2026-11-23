using System.Text;
using Jellyfin.Plugin.JellyOidc.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JellyOidc.Api;

/// <summary>
/// Public login UI assets for the web client.
/// </summary>
[ApiController]
[Route("JellyOidc/ui")]
public class UiController : ControllerBase
{
    [HttpGet("login.js")]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult LoginScript()
    {
        var config = Plugin.Instance.Configuration;
        var label = LoginUiHelper.GetSignInButtonLabel(config);
        var script = new StringBuilder();
        script.AppendLine("(function(){");
        script.AppendLine("if(!window) return;");
        script.Append("var cfg=").Append(System.Text.Json.JsonSerializer.Serialize(new
        {
            enabled = config.Enabled,
            showButton = config.ShowLoginButton,
            oidcOnly = config.OidcOnly,
            autoRedirect = config.AutoRedirectToOidc,
            label,
            displayName = config.DisplayName,
            allowLinking = config.AllowManualAccountLinking,
            startUrl = "/JellyOidc/OIDC/Start",
            linkUrl = "/JellyOidc/OIDC/Link/Start",
            qcUrl = "/JellyOidc/OIDC/QuickConnect"
        })).AppendLine(";");
        script.AppendLine("""
function jellyOidcButton(text, url, extraClass){
  var btn=document.createElement('button');
  btn.type='button';
  btn.className='emby-button raised block jelly-oidc-btn '+(extraClass||'');
  btn.textContent=text;
  btn.onclick=function(){ window.location.href=url; };
  return btn;
}
function inject(){
  if(!cfg.enabled) return;
  var form=document.querySelector('form');
  if(!form) return;
  if(cfg.oidcOnly || cfg.autoRedirect){
    var hide=form.querySelectorAll('input[type=password],input[name=Pw]');
    hide.forEach(function(el){ el.closest('.inputContainer')?.remove(); el.remove(); });
    if(cfg.autoRedirect){ window.location.href=cfg.startUrl; return; }
  }
  if(cfg.showButton){
    var wrap=document.createElement('div');
    wrap.className='jelly-oidc-sso';
    wrap.appendChild(jellyOidcButton(cfg.label||('Sign in with '+(cfg.displayName||'SSO')), cfg.startUrl, 'jelly-oidc-primary'));
    if(cfg.allowLinking){
      wrap.appendChild(jellyOidcButton('Link existing Jellyfin account', cfg.linkUrl, 'jelly-oidc-link'));
    }
    form.appendChild(wrap);
    var qc=document.createElement('a');
    qc.href=cfg.qcUrl;
    qc.className='jelly-oidc-qc';
    qc.textContent='Quick Connect (mobile/TV)';
    form.appendChild(qc);
  }
}
if(document.readyState==='loading'){document.addEventListener('DOMContentLoaded',inject);}else{inject();}
""");
        script.AppendLine("})();");
        return Content(script.ToString(), "application/javascript", Encoding.UTF8);
    }

    [HttpGet("login.css")]
    [AllowAnonymous]
    public IActionResult LoginCss()
    {
        var css = """
.jelly-oidc-sso{margin-top:1.25em;display:flex;flex-direction:column;gap:0.75em;width:100%;}
.jelly-oidc-btn{width:100%;margin:0!important;font-size:105%;padding:0.65em 1em;border-radius:0.35em;transition:transform .2s ease,opacity .2s ease;}
.jelly-oidc-primary{background:var(--accent, #00a4dc);color:#fff!important;border:0;}
.jelly-oidc-primary:hover{transform:scale(1.02);opacity:0.95;}
.jelly-oidc-link{background:transparent;color:var(--accent, #00a4dc)!important;border:1px solid var(--accent, #00a4dc);}
.jelly-oidc-link:hover{transform:scale(1.01);opacity:0.9;}
.jelly-oidc-qc{display:block;margin-top:0.75em;text-align:center;font-size:0.95em;color:inherit;opacity:0.85;}
.jelly-oidc-disclaimer{font-weight:600;text-align:center;margin:0.5em 0;}
""";
        return Content(css, "text/css", Encoding.UTF8);
    }
}