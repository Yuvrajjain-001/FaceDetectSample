#include "StdAfx.h"
#include "ScoredRect.h"

namespace DetectionManagedLib 
{
ScoredRect::ScoredRect(SCORED_RECT *prect, RectType type)
{
	_prect = new SCORED_RECT (*prect);
	if (!_prect)
	{
		throw "Memory allocation failed";
	}

	_type = type;
}

ScoredRect::~ScoredRect()
{
	if (!_prect)
	{
		delete _prect;
	}
}

void ScoredRect::Render (Drawing::Graphics ^graf, Drawing::Rectangle ^rect, bool fSelected)
{
	if (_prect != NULL)
	{
		Pen ^pen;
		Drawing::Color col;

		if (_type == RectType::Raw)
		{
			if (fSelected)
			{
				col = Color::DarkGreen;
			}
			else
			{
				col = Color::LightGreen;
			}
		}
		else
		{
			if (fSelected)
			{
				col = Color::DarkBlue;
			}
			else
			{
				col = Color::LightBlue;
			}
		}

		pen = gcnew Pen (col, fSelected ? 4.0f : 2.0f);

		if (fSelected)
		{
			pen->DashStyle = System::Drawing::Drawing2D::DashStyle::Dash;
		}

		graf->DrawRectangle(pen, rect->X + X, rect->Y + Y, Width, Height);
	}
}

}