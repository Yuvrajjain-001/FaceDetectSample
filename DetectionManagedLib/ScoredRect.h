#pragma once

#include "wrect.h"

using namespace System;
using namespace System::Drawing;

namespace DetectionManagedLib 
{
public ref class ScoredRect
{
public:

	enum class RectType
	{
		Raw = 0,
		Merged = 1
	};

	ScoredRect(SCORED_RECT *prect, RectType type);
	~ScoredRect();

	property float Score
	{
		float get()
		{
			if (_prect == NULL)
			{
				return 0.0f;
			}
			else
			{
				return _prect->m_score;
			}
		}
	}

	property int X
	{
		int get()
		{
			if (_prect == NULL)
			{
				return 0;
			}
			else
			{
				return _prect->m_rect.m_ixMin;
			}
		}
	}

	property int Y
	{
		int get()
		{
			if (_prect == NULL)
			{
				return 0;
			}
			else
			{
				return _prect->m_rect.m_iyMin;
			}
		}
	}

	property int Width
	{
		int get()
		{
			if (_prect == NULL)
			{
				return 0;
			}
			else
			{
				return _prect->m_rect.Width();
			}
		}
	}

	property int Height
	{
		int get()
		{
			if (_prect == NULL)
			{
				return 0;
			}
			else
			{
				return _prect->m_rect.Height();
			}
		}
	}

	property Drawing::Rectangle ^ Bounds
	{
		Drawing::Rectangle ^ get()
		{
			if (_prect == NULL)
			{
				return nullptr;
			}
			else
			{
				return gcnew Drawing::Rectangle(_prect->m_rect.m_ixMin, _prect->m_rect.m_iyMin,
					_prect->m_rect.Width(), _prect->m_rect.Height());
			}
		}
	}

	property IRECT * IRECT_ptr
	{
		IRECT * get()
		{
			if (_prect == NULL)
			{
				return NULL;
			}
			else
			{
				return &_prect->m_rect;
			}
		}
	}

	virtual String ^ ToString() override
	{
		if (_prect == NULL)
		{
			return gcnew String("");
		}
		else
		{
			Drawing::Rectangle ^rect = gcnew Drawing::Rectangle (_prect->m_rect.m_ixMin, _prect->m_rect.m_iyMin,
				_prect->m_rect.Width(), _prect->m_rect.Height());

			return rect->ToString();
		}
	}

	void Render (Drawing::Graphics ^graf, Drawing::Rectangle ^rect, bool fselected);

private:
	SCORED_RECT	*_prect;
	RectType _type;
};
}