struct ScharrKernels { float3x3 x, y; };

//=============================================
//	    Scharr X	||	      Scharr Y		||
//	  -3,  0,  3	||		-3, -10, -3		||
//	  -10, 0, 10	||		 0,   0,  0		||
//	  -3,  0,  3	||		 3,  10,  3		||
//=============================================
ScharrKernels GetEdgeDetectKernels() {
	ScharrKernels kernels;
	kernels.x = float3x3(-3, -10, -3, 0, 0, 0, 3, 10, 3);
	kernels.y = float3x3(-3, 0, 3, -10, 0, 10, -3, 0, 3);
	return kernels;
}

void DepthBasedOutlines_float(float2 uv, float2 px, out float outline) {
	outline = 0;
	#if defined(UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED)
	ScharrKernels kernels = GetEdgeDetectKernels();
	float gx = 0;
	float gy = 0;
	for(int i = -1; i <= 1; i++) {
		for(int j = -1; j <= 1; j++) {
			if (i == 0 && j == 0) continue;
			float2 offset = float2(i, j) * px;
			float d = SampleSceneDepth(uv + offset);
			d = 1 / (_ZBufferParams.z * d + _ZBufferParams.w); // remap non linear depth between near, far clip values
			gx += d * kernels.x[i+1][j+1];
			gy += d * kernels.y[i+1][j+1];
		}
	}
	float g = sqrt(gx * gx + gy * gy);
	// Feel free to experiment with Edge value in Step()
	outline = step(10, g);
	#endif
}

void NormalBasedOutlines_float(float2 uv, float2 px, out float outline) {
	outline = 0;
	#if defined(UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED)
	ScharrKernels kernels = GetEdgeDetectKernels();
	float gx = 0;
	float gy = 0;
	float3 cn = SampleSceneNormals(uv);
	for(int i = -1; i <= 1; i++) {
		for(int j = -1; j <= 1; j++) {
			if (i == 0 && j == 0) continue;
			float2 offset = float2(i, j) * px;
			float3 n = SampleSceneNormals(uv + offset);
			float d = dot(n, cn);
			gx += d * kernels.x[i+1][j+1];
			gy += d * kernels.y[i+1][j+1];
		}
	}
	float g = sqrt(gx * gx + gy * gy);
	// Feel free to experiment with Edge value in Step()
	outline = step(2, g);
	#endif
}