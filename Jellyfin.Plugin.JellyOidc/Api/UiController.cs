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
  btn.className='jelly-oidc-btn '+(extraClass||'');
  btn.textContent=text;
  btn.onclick=function(){ window.location.href=url; };
  return btn;
}
function jellyOidcOr(){
  var div=document.createElement('div');
  div.className='jelly-oidc-or';
  div.setAttribute('aria-hidden','true');
  var lineBefore=document.createElement('span');
  lineBefore.className='jelly-oidc-or-line';
  var label=document.createElement('span');
  label.className='jelly-oidc-or-label';
  label.textContent='OR';
  var lineAfter=document.createElement('span');
  lineAfter.className='jelly-oidc-or-line';
  div.append(lineBefore, label, lineAfter);
  return div;
}
function inject(){
  if(!cfg.enabled) return;
  var form=document.querySelector('form');
  if(!form) return;
  if(document.querySelector('.jelly-oidc-sso')) return;
  if(cfg.oidcOnly || cfg.autoRedirect){
    var hide=form.querySelectorAll('input[type=password],input[name=Pw]');
    hide.forEach(function(el){ el.closest('.inputContainer')?.remove(); el.remove(); });
    if(cfg.autoRedirect){ window.location.href=cfg.startUrl; return; }
  }
  if(cfg.showButton){
    var submit=form.querySelector('button[type=submit],input[type=submit]');
    var wrap=document.createElement('div');
    wrap.className='jelly-oidc-sso';
  if(!cfg.oidcOnly && submit){
      wrap.appendChild(jellyOidcOr());
    }
    wrap.appendChild(jellyOidcButton(cfg.label||('Sign in with '+(cfg.displayName||'SSO')), cfg.startUrl, 'jelly-oidc-primary'));
    if(cfg.allowLinking){
      wrap.appendChild(jellyOidcButton('Link existing Jellyfin account', cfg.linkUrl, 'jelly-oidc-link'));
    }
    if(submit && submit.parentNode){
      submit.parentNode.insertBefore(wrap, submit.nextSibling);
    }else{
      form.appendChild(wrap);
    }
    var qc=document.createElement('a');
    qc.href=cfg.qcUrl;
    qc.className='jelly-oidc-qc';
    qc.textContent='Quick Connect (mobile/TV)';
    wrap.appendChild(qc);
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
.jelly-oidc-sso{margin-top:0.5em;display:flex;flex-direction:column;align-items:stretch;gap:0.75em;width:100%;}
.jelly-oidc-or{display:flex;align-items:center;gap:0.75em;width:100%;margin:1.25em 0 0.85em;}
.jelly-oidc-or-line{flex:1;height:1px;background:rgba(127,127,127,0.45);}
.jelly-oidc-or-label{font-size:0.8rem;font-weight:600;letter-spacing:0.08em;opacity:0.75;text-transform:uppercase;}
.jelly-oidc-btn{display:block;box-sizing:border-box;width:100%;margin:0!important;padding:0.85em 1.5em;border:0;border-radius:9999px;font-size:1rem;font-weight:700;line-height:1.25;cursor:pointer;transition:transform .2s ease,opacity .2s ease,box-shadow .2s ease;}
.jelly-oidc-primary{background:#dcdcdc;color:#121212!important;box-shadow:0 1px 2px rgba(0,0,0,0.12);}
.jelly-oidc-primary:hover{transform:scale(1.01);opacity:0.94;}
.jelly-oidc-primary:active{transform:scale(0.99);}
.jelly-oidc-link{background:rgba(220,220,220,0.12);color:inherit!important;border:1px solid rgba(127,127,127,0.45);font-weight:600;}
.jelly-oidc-link:hover{transform:scale(1.01);opacity:0.9;}
.jelly-oidc-qc{display:block;margin-top:0.35em;text-align:center;font-size:0.9em;color:inherit;opacity:0.75;text-decoration:none;}
.jelly-oidc-qc:hover{opacity:1;text-decoration:underline;}
.jelly-oidc-disclaimer{font-weight:600;text-align:center;margin:0.5em 0;}
""";
        return Content(css, "text/css", Encoding.UTF8);
    }
}