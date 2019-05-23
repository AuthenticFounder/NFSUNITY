using UnityEngine;
using System.Collections;
using System.Linq; // used for Sum of array
using System;

[ExecuteInEditMode]
public class AssignSplatmap : MonoBehaviour
{
    [Range(0,1)]
    public float splat0Range = 0.25f;
    [Range(0, 1)]
    public float splat1Range = 0.5f;
    [Range(0, 1)]
    public float splat2Range = 0.75f;
    
    //[Range(0, 1)]
    //public float splat3Range = 0.75f;
    [Range(0, 1)]
    public float slope = 0.5f;
    //[Range(0, 1)]
    //public float splatLevel3 = 0.75f;
    private void OnEnable()
    {
        Apply();
    }

    void Apply()
    {
        // Get the attached terrain component
        Terrain terrain = GetComponent<Terrain>();

        // Get a reference to the terrain data
        TerrainData terrainData = terrain.terrainData;

        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));

                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrainData.GetInterpolatedNormal(y_01, x_01);

                // Calculate the steepness of the terrain
                float steepness = terrainData.GetSteepness(y_01, x_01);
                Vector3 direction = terrainData.GetInterpolatedNormal(y, x);                      //Get the DIRECTION at this point: returns the direction of the normal as a Vector3


                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];

                float slopeFactor = Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / slope));

                float elevation = height / terrainData.heightmapHeight;
                float splatFactor = (1 - slopeFactor);
                splatWeights[0] = (elevation < splat0Range) ? splatFactor : 0;
                splatWeights[2] = (elevation > splat0Range && elevation < splat1Range) ? splatFactor : 0;
                splatWeights[3] = (elevation > splat1Range) ? splatFactor : 0;
                splatWeights[1] = slopeFactor;
                // splatWeights[3] = (elevation > splat1Range && elevation < splat2Range) ? 1 : 0;
                //splatWeights[1] = (height > splat0Range && height < splat1Range) ? 1 : 0;
                //splatWeights[2] = (height > splat1Range && height < splat2Range) ? 1 : 0; ;// terrainData.heightmapHeight > (splatLevel2) ? (1 - slopeFactor) : 0;
                // splatWeights[3] = (height > splat2Range && height < 1) ? 1 : 0; ;// height > 0.5f ? 1 : 0;


                // CHANGE THE RULES BELOW TO SET THE WEIGHTS OF EACH TEXTURE ON WHATEVER RULES YOU WANT
                /*
                // Texture[0] has constant influence
                splatWeights[0] = terrainData.heightmapHeight * 0.25f;//splatLevel2;

                // Texture[1] is stronger at lower altitudes
                splatWeights[1] = Mathf.Clamp01((terrainData.heightmapHeight - height));

                // Texture[2] stronger on flatter terrain
                // Note "steepness" is unbounded, so we "normalise" it by dividing by the extent of heightmap height and scale factor
                // Subtract result from 1.0 to give greater weighting to flat surfaces
                splatWeights[2] = terrainData.heightmapHeight * 0.75f;// - Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / 5.0f));

                // Texture[3] increases with height but only on surfaces facing positive Z axis 
                splatWeights[3] = terrainData.heightmapHeight * 1.0f;//height * Mathf.Clamp01(normal.z);
                */
                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
    /*
    //public TextureSetting[] Textures;
    void Start2()
    {
        if (this.GetComponent<Terrain>() == null)   // if this game obeject does not have a terrain attached, well then fuckit dont run
        {
            return;
        }

        var terrain = this.gameObject.GetComponent<Terrain>();                                                              //create a neat reference to our terrain
        var terrainData = terrain.terrainData;                                                                              //create a neat reference to our terrain data

        terrainData.splatPrototypes = Textures.Select(s => new SplatPrototype { texture = s.Texture }).ToArray();                   //Get all the textures and assign it to the terrain's spaltprototypes
        terrainData.RefreshPrototypes();                                                                                            //gotta refresh my terraindata's prototypes after its manipulated

        int splatLengths = terrainData.splatPrototypes.Length;
        int alphaMapResolution = terrainData.alphamapResolution;
        int alphaMapHeight = terrainData.alphamapResolution;
        int alphaMapWidth = terrainData.alphamapResolution;

        var splatMap = new float[alphaMapResolution, alphaMapResolution, splatLengths];       //create a new splatmap array equal to our map's, we will store our new splat weights in here, then assight it to the map later
        var heights = terrainData.GetHeights(0, 0, alphaMapWidth, alphaMapHeight);                                 //get all the height points for the terrain... this will be where ware are going paint our textures on

        for (var zRes = 0; zRes < alphaMapHeight; zRes++)
        {
            for (var xRes = 0; xRes < alphaMapWidth; xRes++)
            {
                var splatWeights = new float[splatLengths];                                             //create a temp array to store all our 'none-normalised weights'
                var normalizedX = (float)xRes / (alphaMapWidth - 1);                        //gets the normalised X position based on the map resolution                     
                var normalizedZ = (float)zRes / (alphaMapHeight - 1);                       //gets the normalised Y position based on the map resolution 
                var randomBlendNoise = ReMap(Mathf.PerlinNoise(xRes * .8f, zRes * .5f), 0, 1, .8f, 1);  //Get a random perlin value

                float angle = terrainData.GetSteepness(normalizedX, normalizedZ);                       //Get the ANGLE/STEEPNESS at this point: returns the angle between 0 and 90
                Vector3 direction = terrainData.GetInterpolatedNormal(xRes, zRes);                      //Get the DIRECTION at this point: returns the direction of the normal as a Vector3
                float elevation = heights[zRes, xRes];                                                  //Get the HEIGHT at this point: return between 0 and 1 (0=lowest trough, .5f=Water level. 1f=highest peak)
                float perlinElevation = heights[zRes, xRes] * randomBlendNoise;                         //Get a semi random height based on perlin noise, this is to give a more random blend, rather than straight horizontal lines.

                for (var i = 0; i < Textures.Length; i++)                                               //Loop through all our trextures and apply them accoding to the rules defined
                {
                    var weighting = 0f;                                                                 //set the default weighting to 0, this means that if the image does not meet any of the criteria, then it will have no impact
                    var textureSetting = Textures[i];                                                   //get the setting instance based on index
                    var calculatedHeight = textureSetting.RandomBlend ? perlinElevation : elevation;    //create a new height variable, and make it the actual height, unless the user selected to add a bit of randomness                                      

                    switch (textureSetting.PlacementType)
                    {
                        case PlacementType.Angle:
                            if (Math.Abs(angle - textureSetting.Angle) < textureSetting.Precisision)                        //check if the specified angle is the same as the current angle (allow a variance based on the precision)
                                weighting = textureSetting.Impact;
                            break;
                        case PlacementType.Direction:
                            if (Vector3.SqrMagnitude(direction - textureSetting.Direction) < textureSetting.Precisision)    //check if the specified direction is the same as the current direction (allow a variance based on the precision)
                                weighting = textureSetting.Impact;
                            break;
                        case PlacementType.Elevation:
                            if (Math.Abs(textureSetting.Elevation = calculatedHeight) < textureSetting.Precisision)         //check if the specified elevation is the same as the current elevation (allow a variance based on the precision)
                                weighting = textureSetting.Impact;
                            break;
                        case PlacementType.ElevationRange:
                            if (calculatedHeight > textureSetting.MinRange && calculatedHeight < textureSetting.MaxRange)    //check if the current height is between the specified min and max heights
                                weighting = textureSetting.Impact;
                            break;
                    }

                    splatWeights[i] = weighting;
                }

                #region normalize
                //we need to make sure that the sum of our weights is not greater than 1, so lets normalise it
                var totalWeight = splatWeights.Sum();                               //sum all the splat weights,
                for (int i = 0; i < splatLengths; i++)        //Loop through each splatWeights
                {
                    splatWeights[i] /= totalWeight;                                 //Normalize so that sum of all texture weights = 1
                    splatMap[zRes, xRes, i] = splatWeights[i];                      //Assign this point to the splatmap array
                }
                #endregion
            }
        }

        terrainData.SetAlphamaps(0, 0, splatMap);
    }

    //Get a random periln value within acceptable range
    public float ReMap(float value, float sMin, float sMax, float mMin, float mMax)
    {
        return (value - sMin) * (mMax - mMin) / (sMax - sMin) + mMin;
    }*/
}

[Serializable]
public class TextureSetting
{
    [Tooltip("The texture you want to be placed")]
    public Texture2D Texture;
    [Tooltip("The type of placement")]
    public PlacementType PlacementType;

    [Tooltip("The exact height you want this texture to be displayed (.5 will be the middle of the hieght of the map)")]
    [Range(0, 1)]
    public float Elevation;

    [Tooltip("The angle you want this texture to be displayed at (0-19 deggrees)")]
    [Range(0, 90)]
    public float Angle;

    [Tooltip("The min and the max height you want this texture to be displayed (.5 will be the middle of the hieght of the map)")]
    public Vector3 Direction;


    public float MinRange;
    public float MaxRange;

    [Tooltip("Add some random variations to height based placement, this will give a smoother blend based on height")]
    public bool RandomBlend;

    [Tooltip("Comparing floats gives us a chance of losing floating point values. How precisly do you want your values to be interperetted (0.0001f beeing EXTREMELY precise, 0.9f being irrelevent almost)")]
    public float Precisision;

    public int Impact;
}

public enum PlacementType
{
    Elevation,
    ElevationRange,
    Angle,
    Direction,
}