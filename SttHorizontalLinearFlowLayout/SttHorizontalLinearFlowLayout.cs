using System;
using System.Collections.Generic;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace SttHorizontalLinearFlowLayout
{
    public class SttHorizontalLinearFlowLayout : UICollectionViewFlowLayout
    {
        private CGSize lastCollectionViewSize = CGSize.Empty;

        public nfloat ScalingOffset { get; set; } = 200;
        public nfloat MinimumScaleFactor { get; set; } = 0.9f;
        public bool ScaleItems { get; set; } = true;

        public static SttHorizontalLinearFlowLayout ConfigureLayout(UICollectionView collectionView, CGSize itemSize, nfloat minimumLineSpacig)
        {
            var layout = new SttHorizontalLinearFlowLayout();
            layout.ScrollDirection = UICollectionViewScrollDirection.Horizontal;
            layout.MinimumLineSpacing = minimumLineSpacig;
            layout.ItemSize = itemSize;

            collectionView.DecelerationRate = UIScrollView.DecelerationRateFast;
            collectionView.CollectionViewLayout = layout;

            return layout;
        }

        public override void InvalidateLayout(UICollectionViewLayoutInvalidationContext context)
        {
            base.InvalidateLayout(context);

            if (CollectionView != null)
            {
                var currentCollectionViewSize = CollectionView.Bounds.Size;

                if (!currentCollectionViewSize.Equals(lastCollectionViewSize))
                {
                    ConfigureInset();
                    lastCollectionViewSize = currentCollectionViewSize;
                }
            }
        }

        private void ConfigureInset()
        {
            if (CollectionView != null)
            {
                var inset = CollectionView.Bounds.Size.Width / 2 - ItemSize.Width / 2;
                CollectionView.ContentInset = new UIEdgeInsets(0, inset, 0, inset);
                CollectionView.ContentOffset = new CGPoint(-inset, 0);
            }
        }

        public override CGPoint TargetContentOffset(CGPoint proposedContentOffset, CGPoint scrollingVelocity)
        {
            if (CollectionView == null)
                return proposedContentOffset;

            var collectionViewSize = CollectionView.Bounds.Size;
            var proposedRect = new CGRect(proposedContentOffset.X, 0, collectionViewSize.Width, collectionViewSize.Height);

            var layoutAttributes = LayoutAttributesForElementsInRect(proposedRect);

            if (layoutAttributes == null)
                return proposedContentOffset;

            UICollectionViewLayoutAttributes candidateAttributes = null;
            var proposedContentOffsetCenterX = proposedContentOffset.X + collectionViewSize.Width / 2;

            foreach (var attributes in layoutAttributes)
            {
                if (attributes.RepresentedElementCategory != UICollectionElementCategory.Cell)
                    continue;

                if (candidateAttributes == null)
                {
                    candidateAttributes = attributes;
                    continue;
                }

                if (Math.Abs(attributes.Center.X - proposedContentOffsetCenterX) < Math.Abs(candidateAttributes.Center.X - proposedContentOffsetCenterX))
                    candidateAttributes = attributes;
            }

            if (candidateAttributes == null)
                return proposedContentOffset;

            var newOffsetX = candidateAttributes.Center.X - CollectionView.Bounds.Size.Width / 2;

            var offset = newOffsetX - CollectionView.ContentOffset.X;

            if ((scrollingVelocity.X < 0 && offset > 0) || (scrollingVelocity.X > 0 && offset < 0))
            {
                var pageWidth = ItemSize.Width + MinimumLineSpacing;
                newOffsetX += scrollingVelocity.X > 0 ? pageWidth : -pageWidth;
            }

            return new CGPoint(newOffsetX, proposedContentOffset.Y);
        }

        public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
        {
            return true;
        }

        public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
        {
            if (!ScaleItems || CollectionView == null)
                return base.LayoutAttributesForElementsInRect(rect);


            var superAttributes = base.LayoutAttributesForElementsInRect(rect);

            if (superAttributes == null)
                return null;

            var contentOffset = CollectionView.ContentOffset;
            var size = CollectionView.Bounds.Size;

            var visibleRect = new CGRect(contentOffset.X, contentOffset.Y, size.Width, size.Height);
            var visibleCenterX = visibleRect.GetMidX();

            var newAttributeArray = new List<UICollectionViewLayoutAttributes>();

            foreach (var item in superAttributes)
            {
                var newAttributes = (UICollectionViewLayoutAttributes)item.Copy();
                var distanceFromCenter = visibleCenterX - newAttributes.Center.X;
                var absDistanceFromCenter = Math.Min(Math.Abs(distanceFromCenter), ScalingOffset);
                var scale = absDistanceFromCenter * (MinimumScaleFactor - 1) / ScalingOffset + 1;
                newAttributes.Transform3D = CATransform3D.Identity.Scale((nfloat)scale, (nfloat)scale, 1);

                newAttributeArray.Add(newAttributes);
            }

            return newAttributeArray.ToArray();
        }
    }
}
