using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Color Palette")]
[Serializable]
public class ColorPalette : ScriptableObject
{
    [SerializeField]
    private string paletteName;

    [SerializeField]
    private Color[] colors = new Color[8];

    [SerializeField]
    private string[] hexCodes = new string[8];

    private void Awake()
    {
        GenerateHexCodes();

    }
    public ColorPalette(string name, Color[] colors)
    {
        paletteName = name;
        this.colors = colors;
        GenerateHexCodes();
    }  
    public ColorPalette(Color[] colors)
    {
        paletteName = "un_named";
        this.colors = colors;
        GenerateHexCodes();
    }
    public ColorPalette() { }

    public void GenerateHexCodes()
    {

        hexCodes = new string[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i].a = 1;
            hexCodes[i] = "#" + ColorUtility.ToHtmlStringRGB(colors[i]);
        }
    }
    public void Initialize(string n, Color[] c)
    {
        paletteName = n;
        colors = c;
        GenerateHexCodes();

    }
    public Color this[int index]
    {
        get { return colors[index]; }
        set
        {
            colors[index] = value;
            hexCodes[index] = ColorUtility.ToHtmlStringRGB(value);
        }
    }

    public string GetHexCode(int index)
    {
        return hexCodes[index];
    }
    public void SetHexCode(int index, string hexCode)
    {
        hexCodes[index] = hexCode;
    }
    public Color[] GetColors()
    {
        return colors;
    }
    public string GetName()
    {
        return name;
    }
    public Color[] PopulateColors()
    {
        Color[] colors = new Color[hexCodes.Length];

        for (int i = 0; i < hexCodes.Length; i++)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(hexCodes[i], out color))
            {
                colors[i] = color;
            }
            else
            {
                colors[i] = Color.white;
            }
        }

        return colors;
    }


    public ColorPaletteSerializable ToSerializable()
    {
        ColorPaletteSerializable colorPaletteSerializable = new ColorPaletteSerializable(paletteName, hexCodes);

        return colorPaletteSerializable;
    }

    public void SerializableToSO(ColorPaletteSerializable c)
    {
        paletteName = c.GetName();
        this.hexCodes = c.GetHexCodes();
        this.colors = PopulateColors();
    }



}

[Serializable]
public class ColorPaletteSerializable
{
    [SerializeField]
    private string name;

    [SerializeField]
    private string[] hexCodes = new string[8]; // Array to store the hex codes

    public ColorPaletteSerializable(string name, string[] hexCodes)
    {
        this.name = name;
        this.hexCodes = hexCodes;

    }
    public string GetName()
    {
        return name;

    }
    public string[] GetHexCodes()
    {
        return hexCodes;
    }


}