using System.Text;
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
        var script = new StringBuilder();
        script.AppendLine("(function(){");
        script.AppendLine("if(!window) return;");
        script.Append("var cfg=").Append(System.Text.Json.JsonSerializer.Serialize(new
        {
            enabled = config.Enabled,
            showButton = config.ShowLoginButton,
            oidcOnly = config.OidcOnly,
            autoRedirect = config.AutoRedirectToOidc,
            label = config.ButtonLabel,
            startUrl = "/JellyOidc/OIDC/Start",
            qcUrl = "/JellyOidc/OIDC/QuickConnect"
        })).AppendLine(";");
        script.AppendLine("""
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
    var btn=document.createElement('button');
    btn.type='button';
    btn.className='raised block';
    btn.textContent=cfg.label||'Sign in with SSO';
    btn.style.marginTop='1em';
    btn.onclick=function(){ window.location.href=cfg.startUrl; };
    form.appendChild(btn);
    var qc=document.createElement('a');
    qc.href=cfg.qcUrl;
    qc.textContent='Quick Connect (mobile/TV)';
    qc.style.display='block';
    qc.style.marginTop='0.5em';
    qc.style.textAlign='center';
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
.jelly-oidc-sso button.raised.block{width:100%;}
.jelly-oidc-sso a{color:var(--accent, #00a4dc);}
""";
        if (Plugin.Instance.Configuration.UseJellyfinThemeVars)
        {
            css += """
button.raised.block{background:var(--accent, #00a4dc);color:#fff;border:0;}
""";
        }

        return Content(css, "text/css", Encoding.UTF8);
    }
}