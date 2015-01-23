using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace DiacloLib.Importer
{
    public struct PaletteTranslation
    {
        /* Quote ArthurDent http://www.thedark5.com/forum/forum_posts.asp?TID=669&PN=1&TPN=4
         * - TRN files are Palette Translation files. Read on.
        Palette: A file consisted of colour values. For Diablo they have the extension PAL and each entry is 3 BYTES long. One BYTE per Red/Green/Blue value. Total length is 256 triads. This gives a 256-colour palette.
        Raw Image Data: This are the BYTES that refer to pixels and are found inside CEL/CL2 files. Each BYTE is a Palette index. Leaving transparent bytes aside, the game takes the value from the Raw Image Data BYTE and cross-references it with the current Palette file. A colour (R/G/B) value is returned and the pixel in question is painted with that colour.
        Palette Translations: TRN files contain palette indices. All are 256 BYTES long since all palettes are 256-colour. Their purpose is to save resources by providing a means of deriving Palette variations without having to use extra Palettes for this purpose. When a Raw Image Data BYTE'S value is read, it is cross referenced with the current TRN file and a new index is returned which is in turn either fed into the next TRN for possible further alteration or cross-referenced with the palette to get a colur value.
        NULL.trn: This TRN file provided with TDG is a default TRN file. In other words each of it's 256 BYTES has the value of it's own offset. Such a TRN file will alter nothing when a Raw Image Data BYTE is filtered through it.
        - Example: A Raw Image Byte is read from a CEL/CL2 file. It's value is a palette index. Let's say it has a value of 10. The current palette will have a 3 BYTE entry for it's 10th element. Assume it is blue. If a TRN is used then we would look at the 10th element of that TRN and reqad the value. Let's say 20. This number would then be used to read a different colour value from the palette, ergo the palette's 20th element which would be another colour. In that scenario, a TRN file was used to effectively provide a different palette, i.e. a palette variation. The "translation" of the Palette indices through the TRN values gave us a new pallete without the need to utilise a new PAL file.
         */
        byte[] entry { get; set; }
        public static PaletteTranslation NullTranslation() {
            //Create a default translation (no-change)
            PaletteTranslation ret = new PaletteTranslation();
            for (int i = 0; i < 256; i++)
            {
                ret.entry[i] = (byte)i;
            }
            return ret;
        }
    }
    public class Palette
    {
        
        public static byte[] DefaultPalette = new byte[768]
        {
            0, 0, 0, 200, 126, 53, 188, 126, 86, 188, 118, 48, 177, 119, 80, 170,
            119, 91, 180, 117, 55, 168, 121, 53, 171, 115, 71, 166, 115, 80, 161, 106,
            74, 185, 94, 39, 171, 99, 68, 115, 116, 147, 164, 103, 38, 150, 102, 82,
            147, 107, 48, 150, 99, 71, 218, 48, 61, 139, 107, 48, 111, 107, 131, 150,
            97, 51, 106, 104, 141, 219, 36, 59, 139, 99, 48, 213, 36, 61, 111, 99,
            125, 213, 36, 48, 131, 99, 48, 133, 93, 74, 116, 95, 108, 98, 100, 126,
            207, 36, 48, 133, 94, 36, 135, 88, 57, 200, 36, 48, 200, 36, 36, 194,
            36, 48, 194, 36, 36, 188, 36, 48, 94, 90, 111, 188, 36, 36, 180, 36,
            48, 86, 86, 126, 180, 36, 36, 111, 81, 54, 188, 22, 36, 100, 83, 76,
            93, 79, 97, 114, 71, 75, 180, 22, 36, 171, 30, 38, 78, 81, 106, 106,
            76, 36, 76, 80, 74, 158, 30, 36, 78, 80, 61, 89, 71, 75, 137, 41,
            42, 73, 71, 104, 80, 71, 71, 75, 71, 80, 73, 69, 89, 83, 71, 42,
            150, 22, 22, 91, 59, 72, 74, 71, 61, 0, 0, 0, 101, 51, 51, 61,
            71, 71, 80, 61, 61, 61, 71, 61, 139, 22, 22, 71, 61, 80, 61, 71,
            48, 53, 63, 103, 71, 61, 71, 71, 61, 61, 74, 61, 48, 131, 22, 22,
            76, 61, 30, 59, 61, 80, 61, 61, 71, 61, 61, 61, 120, 26, 22, 61,
            61, 48, 48, 61, 71, 61, 61, 36, 71, 48, 72, 74, 48, 61, 48, 61,
            61, 50, 54, 89, 61, 61, 22, 48, 61, 48, 48, 61, 33, 61, 48, 61,
            73, 44, 39, 61, 48, 48, 61, 48, 36, 41, 48, 80, 48, 48, 61, 50,
            45, 71, 48, 48, 48, 48, 48, 36, 36, 48, 48, 36, 48, 36, 48, 36,
            61, 100, 0, 5, 54, 39, 18, 50, 36, 48, 35, 39, 61, 50, 36, 36,
            36, 36, 48, 36, 36, 36, 36, 36, 22, 22, 36, 36, 22, 36, 22, 36,
            22, 48, 38, 22, 36, 22, 28, 48, 38, 22, 20, 22, 22, 36, 22, 22,
            22, 22, 22, 0, 0, 22, 22, 22, 0, 0, 13, 0, 22, 0, 0, 0,
            183, 183, 255, 120, 120, 255, 64, 64, 254, 5, 5, 243, 0, 0, 207, 0,
            0, 166, 0, 0, 120, 0, 0, 50, 255, 183, 183, 255, 120, 120, 254, 64,
            64, 244, 0, 0, 207, 0, 0, 171, 0, 0, 123, 0, 0, 63, 0, 0,
            255, 253, 183, 255, 253, 120, 254, 252, 64, 244, 241, 0, 211, 211, 0, 162,
            162, 0, 120, 118, 0, 50, 50, 0, 254, 207, 184, 255, 167, 120, 254, 137,
            64, 244, 109, 0, 214, 98, 0, 170, 76, 0, 120, 51, 0, 57, 20, 0,
            238, 216, 216, 226, 198, 198, 216, 182, 182, 207, 170, 170, 199, 157, 157, 190,
            144, 144, 188, 123, 123, 180, 106, 106, 166, 98, 98, 151, 89, 89, 136, 80,
            80, 119, 71, 71, 101, 61, 61, 82, 50, 50, 53, 33, 33, 30, 20, 20,
            215, 219, 240, 198, 202, 226, 183, 188, 213, 173, 178, 203, 163, 168, 193, 151,
            156, 184, 134, 143, 178, 121, 131, 168, 111, 121, 154, 100, 109, 142, 89, 98,
            127, 78, 86, 113, 66, 73, 98, 50, 57, 75, 31, 38, 53, 16, 20, 30,
            255, 235, 187, 243, 222, 167, 230, 212, 155, 218, 202, 147, 206, 190, 139, 193,
            179, 131, 177, 166, 126, 163, 155, 117, 150, 142, 106, 137, 128, 95, 124, 114,
            83, 105, 97, 68, 89, 80, 55, 80, 69, 45, 51, 42, 22, 42, 28, 0,
            255, 234, 199, 247, 216, 176, 238, 199, 155, 230, 183, 143, 221, 167, 130, 214,
            153, 115, 218, 129, 82, 214, 108, 58, 197, 100, 53, 180, 91, 48, 161, 81,
            41, 142, 71, 36, 123, 62, 30, 95, 47, 22, 66, 33, 11, 35, 16, 0,
            255, 207, 207, 247, 176, 176, 238, 154, 154, 233, 139, 139, 227, 124, 124, 220,
            106, 106, 214, 88, 88, 208, 68, 68, 191, 62, 62, 173, 57, 57, 154, 50,
            50, 134, 44, 44, 112, 38, 38, 89, 31, 31, 63, 24, 24, 30, 16, 16,
            246, 246, 246, 231, 231, 231, 218, 218, 218, 203, 203, 203, 186, 186, 186, 174,
            174, 174, 161, 161, 161, 146, 146, 146, 134, 134, 134, 122, 122, 122, 109, 109,
            109, 93, 93, 93, 76, 76, 76, 57, 57, 57, 38, 38, 38, 255, 255, 255
        };
         
        private byte[] entry;
        private Color[] color;
        /// <summary>
        /// Create a palette object from a 768 byte palette (rgbrgb...)
        /// </summary>
        /// <param name="data"></param>
        public Palette(byte[] data)
        {
            this.entry = data;
            this.color = new Color[256];
            createColors();
        }
        public Palette()
        {
            entry = new byte[256*3];
        }
        public void SetColor(int index, Color c)
        {
            this.entry[index * 3] = c.R;
            this.entry[index * 3 + 1] = c.G;
            this.entry[index * 3 + 2] = c.B;
            this.color[index] = c;
        }
        public Color GetColor(int index)
        {
            return this.color[index];
        }
        private void createColors()
        {
            for (int i = 0; i < 256; i++)
            {
                this.color[i] = new Color(entry[i*3], entry[i*3 + 1], entry[i*3 + 2], 255);
                if(this.entry[i*3] + this.entry[i*3+1] + this.entry[i*3+2] == 765)
                    this.color[255] = new Color(0, 0, 0, 0); //Transparent
            }
            
        }

    }
}
