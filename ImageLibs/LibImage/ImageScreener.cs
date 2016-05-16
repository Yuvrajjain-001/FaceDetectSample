using System;
using System.Collections;

namespace Dpu.ImageProcessing
{
	using PixelType = System.Single;

	public interface ImageScreener
	{
		bool Include(int c, int r, PixelType val);
	}

	public class ComponentIdScreener : ImageScreener
	{
		public ComponentIdScreener(DiscreteImage iimg, bool includeComponentIds)
		{
			this.iimg = iimg;
			this.includeComponentIds = includeComponentIds;
			this.componentIds = new Hashtable();
		}

		public ComponentIdScreener(DiscreteImage iimg, int componentId, bool includeComponentIds)
		{
			this.iimg = iimg;
			this.includeComponentIds = includeComponentIds;
			this.componentIds = new Hashtable();
			AddComponentId(componentId);
		}

		public void AddComponentId(int componentId)
		{
			componentIds[componentId] = true;
		}

		public bool Include(int c, int r, PixelType val)
		{
			return includeComponentIds == componentIds.Contains(iimg.GetPixel(c, r));
		}

		public DiscreteImage iimg;
		bool includeComponentIds;
		Hashtable componentIds;
	}
}
