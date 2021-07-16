using System;
using System.Numerics;

namespace Area_Randomizer
{
    public class Area
    {
        public Vector2 fullArea;
        public Vector2 size;
        public Vector2 position;
        Random rdm = new Random();
        public Area(Vector2 inputSize, Vector2 inputPosition)
        {
            size = inputSize;
            position = inputPosition;
        }
        public Area(Vector2 area, bool enableAspectRatio, bool enableIndependantRandomization, int area_MinX, int area_MaxX, int area_MinY, int area_MaxY, float? aspectRatio)
        {
            fullArea = area;
            generateArea(enableAspectRatio, enableIndependantRandomization, area_MinX, area_MaxX, area_MinY, area_MaxY, aspectRatio);
        }
        public string toString()
        {
            return $"Position: [{position.X}; {position.Y}], Size: [{size.X};{size.Y}]";
        }
        public Vector2 toTopLeft() 
        {
            return position - (0.5f * size);
        }
        public void Update(Vector2 sizeUpdate, Vector2 positionUpdate)
        {
            size = size + sizeUpdate;
            position = position + positionUpdate;
        }
        /*
            TODO
                - Rework this part to generate size depending on position instead?
                - Would allow for static position
        */
        public void generateArea(bool enableAspectRatio, bool enableIndependantRandomization, int area_MinX, int area_MaxX, int area_MinY, int area_MaxY, float? aspectRatio)
        {
            float sizeMultiplier = (float)rdm.Next(area_MinX, area_MaxX) / 100;
            if (enableIndependantRandomization) 
            {
                float sizeMultiplierY = (float)rdm.Next(area_MinY, area_MaxY) / 100;
                size = fullArea * new Vector2(sizeMultiplier, sizeMultiplierY);
            }
            else
            {
                if (enableAspectRatio)
                {
                    size = new Vector2(sizeMultiplier * fullArea.X, (sizeMultiplier * fullArea.X) / (float)aspectRatio);
                }
                else
                {
                    size = fullArea * sizeMultiplier;
                }
            }
            Vector2 minPos = (size / 2) / fullArea;
            Vector2 maxPos = Vector2.One - minPos;
            position = (minPos + ((maxPos - minPos) * new Vector2((float)rdm.NextDouble(), (float)rdm.NextDouble()))) * fullArea;
        }
    }
}