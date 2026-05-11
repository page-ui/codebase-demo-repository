using Page.Ui.SvelteRender.Models;

namespace Page.Ui.SvelteRender.Services;

public interface INodeRenderService
{
    Task<RenderResponse> CompileAsync(RenderRequest request);
}
