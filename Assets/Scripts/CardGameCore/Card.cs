﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class Card : IComparable<Card>, IEquatable<Card>
{
    public static Card Blank {
        get { return new Card(string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertyDefValuePair>()); }
    }

    public string Id { get; private set; }

    public string Name { get; private set; }

    public string SetCode { get; private set; }

    protected Dictionary<string, PropertyDefValuePair> Properties { get; private set; }

    public HashSet<CardModel> ModelsUsingImage { get; private set; }

    public bool IsLoadingImage { get; set; }

    private UnityEngine.Sprite _imageSprite;

    public Card(string id, string name, string setCode, Dictionary<string,PropertyDefValuePair> properties)
    {
        Id = id.Clone() as string;
        Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : string.Empty;
        SetCode = !string.IsNullOrEmpty(setCode) ? setCode.Clone() as string : Set.DefaultCode;
        if (properties == null)
            properties = new Dictionary<string, PropertyDefValuePair>();
        Properties = properties;
        this.Properties = this.CloneProperties();
        ModelsUsingImage = new HashSet<CardModel>();
    }

    public Dictionary<string, PropertyDefValuePair> CloneProperties()
    {
        Dictionary<string, PropertyDefValuePair> clone = new Dictionary<string, PropertyDefValuePair>();
        foreach (KeyValuePair<string, PropertyDefValuePair> property in Properties)
            clone.Add((string)property.Key.Clone(), property.Value.Clone() as PropertyDefValuePair);
        return clone;
    }

    public string GetPropertyValueString(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !Properties.ContainsKey(propertyName))
            return string.Empty;
        
        EnumDef enumDef = CardGameManager.Current.Enums.Where(def => def.Property.Equals(propertyName)).FirstOrDefault();
        if (enumDef != null) {
            PropertyDefValuePair property = Properties [propertyName];
            int lookupKeys;
            string stringValue;
            if (((property.Def.Type == PropertyType.EnumList || enumDef.LookupEqualsValue) && EnumDef.TryParseInt(property.Value, out lookupKeys)) || enumDef.ReverseLookup.TryGetValue(property.Value, out lookupKeys))
                return enumDef.GetStringFromLookupKeys(lookupKeys);
            if (enumDef.Values.TryGetValue(property.Value, out stringValue))
                return stringValue;
        }

        return Properties [propertyName] != null ? Properties [propertyName].Value : string.Empty;
    }

    public int GetPropertyValueInt(string propertyName)
    {
        PropertyDefValuePair property;
        if (string.IsNullOrEmpty(propertyName) || !Properties.TryGetValue(propertyName, out property))
            return 0; 
        
        int intValue;
        bool isHex = property.Value.StartsWith("0x");
        int.TryParse(isHex ? property.Value.Substring(2) : property.Value, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue);
        return intValue;
    }

    public int GetPropertyValueEnum(string propertyName)
    {
        PropertyDefValuePair property;
        if (string.IsNullOrEmpty(propertyName) || !Properties.TryGetValue(propertyName, out property))
            return 0; 
        
        EnumDef enumDef = CardGameManager.Current.Enums.Where(def => def.Property.Equals(propertyName)).FirstOrDefault();
        if (enumDef == null)
            return 0;

        int enumValue;
        if (((property.Def.Type == PropertyType.EnumList || enumDef.LookupEqualsValue) && EnumDef.TryParseInt(property.Value, out enumValue)) || enumDef.ReverseLookup.TryGetValue(property.Value, out enumValue))
            return enumValue;
        return 0;
    }

    public int CompareTo(Card other)
    {
        if (other == null)
            return -1;

        foreach (PropertyDefValuePair property in Properties.Values) {
            int comparison = 0;
            switch (property.Def.Type) {
                case PropertyType.Enum:
                case PropertyType.Integer:
                    int thisValue = GetPropertyValueInt(property.Def.Name);
                    int otherValue = other.GetPropertyValueInt(property.Def.Name);
                    comparison = thisValue.CompareTo(otherValue);
                    break;
                case PropertyType.String:
                default:
                    comparison = property.Value.CompareTo(other.Properties [property.Def.Name].Value);
                    break;
            }
            if (comparison != 0)
                return comparison;
        }
        return 0;
    }

    public bool Equals(Card other)
    {
        if (other == null)
            return false;

        return Id.Equals(other.Id);
    }

    public string ImageFileName {
        get { 
            return UnityExtensionMethods.GetSafeFileName(Id + "." + CardGameManager.Current.CardImageFileType);
        }
    }

    public string ImageFilePath {
        get { 
            return UnityExtensionMethods.GetSafeFilePath(CardGameManager.Current.FilePathBase + "/sets/" + SetCode + "/") + ImageFileName;
        }
    }

    public string ImageWebUrl {
        get {
            return 
                string.Format(
                CardGameManager.Current.CardImageUrlFormat, 
                CardGameManager.Current.CardImageUrlBase, Id, CardGameManager.Current.CardImageFileType, Name, SetCode, GetPropertyValueString(CardGameManager.Current.CardImageUrlProperty));
        }
    }

    public UnityEngine.Sprite ImageSprite {
        get {
            return _imageSprite;
        }
        set {
            if (_imageSprite != null) {
                UnityEngine.GameObject.Destroy(_imageSprite.texture);
                UnityEngine.GameObject.Destroy(_imageSprite);
            }
            _imageSprite = value;
        }
    }
}
