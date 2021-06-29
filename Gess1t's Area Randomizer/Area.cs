using System;
using System.Numerics;

namespace Area_Randomizer
{
    class Area
    {
        Vector2 fullArea;
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
            return new Vector2(position.X - (0.5f * size.X), position.Y - (0.5f * size.Y));
        }
        public void Update(Vector2 sizeUpdate, Vector2 positionUpdate)
        {
            size = Vector2.Add(size, sizeUpdate);
            position = Vector2.Add(position, positionUpdate);
        }
        public void generateArea(bool enableAspectRatio, bool enableIndependantRandomization, int area_MinX, int area_MaxX, int area_MinY, int area_MaxY, float? aspectRatio)
        {
            float sizeMultiplier = (float)rdm.Next(area_MinX, area_MaxX) / 100;
            if (enableIndependantRandomization) 
            {
                float sizeMultiplierY = (float)rdm.Next(area_MinY, area_MaxY) / 100;
                size = new Vector2(sizeMultiplier * fullArea.X, sizeMultiplierY * fullArea.X);
            }
            else
            {
                if (enableAspectRatio)
                {
                    size = new Vector2(sizeMultiplier * fullArea.X, (sizeMultiplier * fullArea.X) / (float)aspectRatio);
                }
                else
                {
                    size = new Vector2(sizeMultiplier * fullArea.X, sizeMultiplier * fullArea.Y);
                }
            }
            float minXPos = (size.X / 2) / fullArea.X;
            float maxXPos = 1 - minXPos;
            float minYPos = (size.Y / 2) / fullArea.Y;
            float maxYPos = 1 - minYPos;
            float xPos = ((float)rdm.Next((int)(minXPos * 10000), (int)(maxXPos * 10000)) / 10000) * fullArea.X;
            float yPos = ((float)rdm.Next((int)(minYPos * 10000), (int)(maxYPos * 10000)) / 10000) * fullArea.Y;
            position = new Vector2(xPos, yPos);
        }
    }
}