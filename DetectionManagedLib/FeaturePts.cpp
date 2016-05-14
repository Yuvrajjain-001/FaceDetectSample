#include "StdAfx.h"
#include "FeaturePts.h"

#define MARK_SIZE	4

namespace DetectionManagedLib 
{
FeaturePts::FeaturePts(struct FEATUREPTS *pFeaturePts)
{
	_pFeaturePts = pFeaturePts;
}

void FeaturePts::Render (Drawing::Graphics ^graf,  int x, int y)
{
	graf->DrawLine (gcnew Pen(Color::Cyan, 2), 
		x + _pFeaturePts->leye.x - MARK_SIZE,
		y + _pFeaturePts->leye.y - MARK_SIZE,
		x + _pFeaturePts->leye.x + MARK_SIZE,
		y + _pFeaturePts->leye.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Cyan, 2), 
		x + _pFeaturePts->leye.x + MARK_SIZE,
		y + _pFeaturePts->leye.y - MARK_SIZE,
		x + _pFeaturePts->leye.x - MARK_SIZE,
		y + _pFeaturePts->leye.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Cyan, 2), 
		x + _pFeaturePts->reye.x - MARK_SIZE,
		y + _pFeaturePts->reye.y - MARK_SIZE,
		x + _pFeaturePts->reye.x + MARK_SIZE,
		y + _pFeaturePts->reye.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Cyan, 2), 
		x + _pFeaturePts->reye.x + MARK_SIZE,
		y + _pFeaturePts->reye.y - MARK_SIZE,
		x + _pFeaturePts->reye.x - MARK_SIZE,
		y + _pFeaturePts->reye.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Green, 2), 
		x + _pFeaturePts->nose.x - MARK_SIZE,
		y + _pFeaturePts->nose.y - MARK_SIZE,
		x + _pFeaturePts->nose.x + MARK_SIZE,
		y + _pFeaturePts->nose.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Green, 2), 
		x + _pFeaturePts->nose.x + MARK_SIZE,
		y + _pFeaturePts->nose.y - MARK_SIZE,
		x + _pFeaturePts->nose.x - MARK_SIZE,
		y + _pFeaturePts->nose.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Red, 2), 
		x + _pFeaturePts->lmouth.x - MARK_SIZE,
		y + _pFeaturePts->lmouth.y - MARK_SIZE,
		x + _pFeaturePts->lmouth.x + MARK_SIZE,
		y + _pFeaturePts->lmouth.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Red, 2), 
		x + _pFeaturePts->lmouth.x + MARK_SIZE,
		y + _pFeaturePts->lmouth.y - MARK_SIZE,
		x + _pFeaturePts->lmouth.x - MARK_SIZE,
		y + _pFeaturePts->lmouth.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Red, 2), 
		x + _pFeaturePts->rmouth.x - MARK_SIZE,
		y + _pFeaturePts->rmouth.y - MARK_SIZE,
		x + _pFeaturePts->rmouth.x + MARK_SIZE,
		y + _pFeaturePts->rmouth.y + MARK_SIZE);

	graf->DrawLine (gcnew Pen(Color::Red, 2), 
		x + _pFeaturePts->rmouth.x + MARK_SIZE,
		y + _pFeaturePts->rmouth.y - MARK_SIZE,
		x + _pFeaturePts->rmouth.x - MARK_SIZE,
		y + _pFeaturePts->rmouth.y + MARK_SIZE);
}

}
