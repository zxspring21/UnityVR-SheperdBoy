using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public abstract class PostProcessEffectBase : MonoBehaviour 
{
	public bool SupportHDRTextures
	{
		get { return supportHDRTextures; }
	}
	public bool SupportDX11
	{
		get { return supportDX11; }
	}
	public bool IsSupported
	{
		get { return isSupported; }
		set 
		{
			isSupported = value;
			if (!isSupported)
			{
				enabled = false;
			}
		}
	}
	protected bool supportHDRTextures = true;
	protected bool supportDX11 = false;
	protected bool isSupported = true;
	protected Camera effectCamera = null;
	protected Transform effectCameraTransform = null;
	void Start () 
	{
		effectCamera = GetComponent<Camera>();
		effectCameraTransform = effectCamera.transform;
		CheckResources();
	}
	
	void OnEnable () 
	{
		isSupported = true;
	}

	public abstract bool CheckResources();

	public bool CheckSupport(bool needDepth = false, bool needHdr = false)
	{
		isSupported = true;
		supportHDRTextures = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
		supportDX11 = ((SystemInfo.graphicsShaderLevel >= 50) && SystemInfo.supportsComputeShaders);
		
		if (!SystemInfo.supportsImageEffects || !SystemInfo.supportsRenderTextures)
		{
			IsSupported = false;
			return false;
		}		
		
		if(needDepth && !SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth))
		{
			IsSupported = false;
			return false;
		}
		
		if(needDepth)
		{ 
			effectCamera.depthTextureMode |= DepthTextureMode.Depth;
		}
			
		if (needHdr && !supportHDRTextures)
		{
			IsSupported = false;
			return false;
		}

		return true;
	}

	public void ReportAutoDisable () 
	{
		Debug.LogWarning ("The image effect " + this.ToString() + " has been disabled as it's not supported on the current platform.");
	}

	public void DrawBorder (RenderTexture dest, Material material )
	{
		float x1 = 0f;
		float x2 = 0f;
		float y1 = 0f;
		float y2 = 0f;
		float texY1 = 0f;
		float texY2 = 0f;

		RenderTexture.active = dest;
        bool invertY = true; // This may need to interact with steamvr flipping somehow
        
		// Set up the simple Matrix
        GL.PushMatrix();
        GL.LoadOrtho();

		int passCounter = 0;
		while (passCounter < material.passCount)
		{
			material.SetPass(passCounter);
	        
	        if (invertY)
	        {
	            texY1 = 1.0f;
				texY2 = 0.0f;
	        }
	        else
	        {
	            texY1 = 0.0f;
				texY2 = 1.0f;
	        }
	        	        
	        // left
	        x1 = 0.0f;
	        x2 = 0.0f + 1.0f/((float)dest.width);
	        y1 = 0.0f;
	        y2 = 1.0f;
	        GL.Begin(GL.QUADS);
	        
	        GL.TexCoord2(0.0f, texY1); GL.Vertex3(x1, y1, 0.1f);
	        GL.TexCoord2(1.0f, texY1); GL.Vertex3(x2, y1, 0.1f);
	        GL.TexCoord2(1.0f, texY2); GL.Vertex3(x2, y2, 0.1f);
	        GL.TexCoord2(0.0f, texY2); GL.Vertex3(x1, y2, 0.1f);
	
	        // right
	        x1 = 1.0f - 1.0f/((float)dest.width);
	        x2 = 1.0f;
	        y1 = 0.0f;
	        y2 = 1.0f;

	        GL.TexCoord2(0.0f, texY1); GL.Vertex3(x1, y1, 0.1f);
	        GL.TexCoord2(1.0f, texY1); GL.Vertex3(x2, y1, 0.1f);
	        GL.TexCoord2(1.0f, texY2); GL.Vertex3(x2, y2, 0.1f);
	        GL.TexCoord2(0.0f, texY2); GL.Vertex3(x1, y2, 0.1f);	        
	
	        // top
	        x1 = 0.0f;
	        x2 = 1.0f;
	        y1 = 0.0f;
	        y2 = 0.0f + 1.0f/((float)dest.height);

	        GL.TexCoord2(0.0f, texY1); GL.Vertex3(x1, y1, 0.1f);
	        GL.TexCoord2(1.0f, texY1); GL.Vertex3(x2, y1, 0.1f);
	        GL.TexCoord2(1.0f, texY2); GL.Vertex3(x2, y2, 0.1f);
	        GL.TexCoord2(0.0f, texY2); GL.Vertex3(x1, y2, 0.1f);
	        
	        // bottom
	        x1 = 0.0f;
	        x2 = 1.0f;
	        y1 = 1.0f - 1.0f/((float)dest.height);
	        y2 = 1.0f;

	        GL.TexCoord2(0.0f, texY1); GL.Vertex3(x1, y1, 0.1f);
	        GL.TexCoord2(1.0f, texY1); GL.Vertex3(x2, y1, 0.1f);
	        GL.TexCoord2(1.0f, texY2); GL.Vertex3(x2, y2, 0.1f);
	        GL.TexCoord2(0.0f, texY2); GL.Vertex3(x1, y2, 0.1f);	
	                	              
	        GL.End();	
			passCounter++;
		}

		GL.PopMatrix();
	}

	public Material CreateMaterial (Shader shader, Material materialToCreate, bool required = false)
	{
		if (!shader)
		{ 
			Debug.Log ("Missing shader in " + this.ToString());
			IsSupported = false;
			return null;
		}
			
		if (materialToCreate && (materialToCreate.shader == shader) && (shader.isSupported)) 
			return materialToCreate;
		
		if (!shader.isSupported) 
		{
			if (required)
			{
				IsSupported = false;
			}
			Debug.Log("The shader " + shader.ToString() + " on effect " + this.ToString() + " is not supported on this platform!");
			return null;
		}
		else 
		{
			materialToCreate = new Material (shader);	
			materialToCreate.hideFlags = HideFlags.DontSave;
			if (materialToCreate)
			{
				return materialToCreate;
			}
			else
			{
				return null;
			}
		}
	}
}
