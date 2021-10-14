// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : CRC64Context.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Checksums.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements a CRC64 algorithm.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2021 Natalia Portillo
// ****************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Checksums
{
    /// <inheritdoc />
    /// <summary>Implements a CRC64 algorithm</summary>
    public sealed class Crc64Context : IChecksum
    {
        /// <summary>ECMA CRC64 polynomial</summary>
        public const ulong CRC64_ECMA_POLY = 0xC96C5795D7870F42;
        /// <summary>ECMA CRC64 seed</summary>
        public const ulong CRC64_ECMA_SEED = 0xFFFFFFFFFFFFFFFF;

        static readonly ulong[][] _ecmaCrc64Table =
        {
            new ulong[]
            {
                0x0000000000000000, 0xB32E4CBE03A75F6F, 0xF4843657A840A05B, 0x47AA7AE9ABE7FF34, 0x7BD0C384FF8F5E33,
                0xC8FE8F3AFC28015C, 0x8F54F5D357CFFE68, 0x3C7AB96D5468A107, 0xF7A18709FF1EBC66, 0x448FCBB7FCB9E309,
                0x0325B15E575E1C3D, 0xB00BFDE054F94352, 0x8C71448D0091E255, 0x3F5F08330336BD3A, 0x78F572DAA8D1420E,
                0xCBDB3E64AB761D61, 0x7D9BA13851336649, 0xCEB5ED8652943926, 0x891F976FF973C612, 0x3A31DBD1FAD4997D,
                0x064B62BCAEBC387A, 0xB5652E02AD1B6715, 0xF2CF54EB06FC9821, 0x41E11855055BC74E, 0x8A3A2631AE2DDA2F,
                0x39146A8FAD8A8540, 0x7EBE1066066D7A74, 0xCD905CD805CA251B, 0xF1EAE5B551A2841C, 0x42C4A90B5205DB73,
                0x056ED3E2F9E22447, 0xB6409F5CFA457B28, 0xFB374270A266CC92, 0x48190ECEA1C193FD, 0x0FB374270A266CC9,
                0xBC9D3899098133A6, 0x80E781F45DE992A1, 0x33C9CD4A5E4ECDCE, 0x7463B7A3F5A932FA, 0xC74DFB1DF60E6D95,
                0x0C96C5795D7870F4, 0xBFB889C75EDF2F9B, 0xF812F32EF538D0AF, 0x4B3CBF90F69F8FC0, 0x774606FDA2F72EC7,
                0xC4684A43A15071A8, 0x83C230AA0AB78E9C, 0x30EC7C140910D1F3, 0x86ACE348F355AADB, 0x3582AFF6F0F2F5B4,
                0x7228D51F5B150A80, 0xC10699A158B255EF, 0xFD7C20CC0CDAF4E8, 0x4E526C720F7DAB87, 0x09F8169BA49A54B3,
                0xBAD65A25A73D0BDC, 0x710D64410C4B16BD, 0xC22328FF0FEC49D2, 0x85895216A40BB6E6, 0x36A71EA8A7ACE989,
                0x0ADDA7C5F3C4488E, 0xB9F3EB7BF06317E1, 0xFE5991925B84E8D5, 0x4D77DD2C5823B7BA, 0x64B62BCAEBC387A1,
                0xD7986774E864D8CE, 0x90321D9D438327FA, 0x231C512340247895, 0x1F66E84E144CD992, 0xAC48A4F017EB86FD,
                0xEBE2DE19BC0C79C9, 0x58CC92A7BFAB26A6, 0x9317ACC314DD3BC7, 0x2039E07D177A64A8, 0x67939A94BC9D9B9C,
                0xD4BDD62ABF3AC4F3, 0xE8C76F47EB5265F4, 0x5BE923F9E8F53A9B, 0x1C4359104312C5AF, 0xAF6D15AE40B59AC0,
                0x192D8AF2BAF0E1E8, 0xAA03C64CB957BE87, 0xEDA9BCA512B041B3, 0x5E87F01B11171EDC, 0x62FD4976457FBFDB,
                0xD1D305C846D8E0B4, 0x96797F21ED3F1F80, 0x2557339FEE9840EF, 0xEE8C0DFB45EE5D8E, 0x5DA24145464902E1,
                0x1A083BACEDAEFDD5, 0xA9267712EE09A2BA, 0x955CCE7FBA6103BD, 0x267282C1B9C65CD2, 0x61D8F8281221A3E6,
                0xD2F6B4961186FC89, 0x9F8169BA49A54B33, 0x2CAF25044A02145C, 0x6B055FEDE1E5EB68, 0xD82B1353E242B407,
                0xE451AA3EB62A1500, 0x577FE680B58D4A6F, 0x10D59C691E6AB55B, 0xA3FBD0D71DCDEA34, 0x6820EEB3B6BBF755,
                0xDB0EA20DB51CA83A, 0x9CA4D8E41EFB570E, 0x2F8A945A1D5C0861, 0x13F02D374934A966, 0xA0DE61894A93F609,
                0xE7741B60E174093D, 0x545A57DEE2D35652, 0xE21AC88218962D7A, 0x5134843C1B317215, 0x169EFED5B0D68D21,
                0xA5B0B26BB371D24E, 0x99CA0B06E7197349, 0x2AE447B8E4BE2C26, 0x6D4E3D514F59D312, 0xDE6071EF4CFE8C7D,
                0x15BB4F8BE788911C, 0xA6950335E42FCE73, 0xE13F79DC4FC83147, 0x521135624C6F6E28, 0x6E6B8C0F1807CF2F,
                0xDD45C0B11BA09040, 0x9AEFBA58B0476F74, 0x29C1F6E6B3E0301B, 0xC96C5795D7870F42, 0x7A421B2BD420502D,
                0x3DE861C27FC7AF19, 0x8EC62D7C7C60F076, 0xB2BC941128085171, 0x0192D8AF2BAF0E1E, 0x4638A2468048F12A,
                0xF516EEF883EFAE45, 0x3ECDD09C2899B324, 0x8DE39C222B3EEC4B, 0xCA49E6CB80D9137F, 0x7967AA75837E4C10,
                0x451D1318D716ED17, 0xF6335FA6D4B1B278, 0xB199254F7F564D4C, 0x02B769F17CF11223, 0xB4F7F6AD86B4690B,
                0x07D9BA1385133664, 0x4073C0FA2EF4C950, 0xF35D8C442D53963F, 0xCF273529793B3738, 0x7C0979977A9C6857,
                0x3BA3037ED17B9763, 0x888D4FC0D2DCC80C, 0x435671A479AAD56D, 0xF0783D1A7A0D8A02, 0xB7D247F3D1EA7536,
                0x04FC0B4DD24D2A59, 0x3886B22086258B5E, 0x8BA8FE9E8582D431, 0xCC0284772E652B05, 0x7F2CC8C92DC2746A,
                0x325B15E575E1C3D0, 0x8175595B76469CBF, 0xC6DF23B2DDA1638B, 0x75F16F0CDE063CE4, 0x498BD6618A6E9DE3,
                0xFAA59ADF89C9C28C, 0xBD0FE036222E3DB8, 0x0E21AC88218962D7, 0xC5FA92EC8AFF7FB6, 0x76D4DE52895820D9,
                0x317EA4BB22BFDFED, 0x8250E80521188082, 0xBE2A516875702185, 0x0D041DD676D77EEA, 0x4AAE673FDD3081DE,
                0xF9802B81DE97DEB1, 0x4FC0B4DD24D2A599, 0xFCEEF8632775FAF6, 0xBB44828A8C9205C2, 0x086ACE348F355AAD,
                0x34107759DB5DFBAA, 0x873E3BE7D8FAA4C5, 0xC094410E731D5BF1, 0x73BA0DB070BA049E, 0xB86133D4DBCC19FF,
                0x0B4F7F6AD86B4690, 0x4CE50583738CB9A4, 0xFFCB493D702BE6CB, 0xC3B1F050244347CC, 0x709FBCEE27E418A3,
                0x3735C6078C03E797, 0x841B8AB98FA4B8F8, 0xADDA7C5F3C4488E3, 0x1EF430E13FE3D78C, 0x595E4A08940428B8,
                0xEA7006B697A377D7, 0xD60ABFDBC3CBD6D0, 0x6524F365C06C89BF, 0x228E898C6B8B768B, 0x91A0C532682C29E4,
                0x5A7BFB56C35A3485, 0xE955B7E8C0FD6BEA, 0xAEFFCD016B1A94DE, 0x1DD181BF68BDCBB1, 0x21AB38D23CD56AB6,
                0x9285746C3F7235D9, 0xD52F0E859495CAED, 0x6601423B97329582, 0xD041DD676D77EEAA, 0x636F91D96ED0B1C5,
                0x24C5EB30C5374EF1, 0x97EBA78EC690119E, 0xAB911EE392F8B099, 0x18BF525D915FEFF6, 0x5F1528B43AB810C2,
                0xEC3B640A391F4FAD, 0x27E05A6E926952CC, 0x94CE16D091CE0DA3, 0xD3646C393A29F297, 0x604A2087398EADF8,
                0x5C3099EA6DE60CFF, 0xEF1ED5546E415390, 0xA8B4AFBDC5A6ACA4, 0x1B9AE303C601F3CB, 0x56ED3E2F9E224471,
                0xE5C372919D851B1E, 0xA26908783662E42A, 0x114744C635C5BB45, 0x2D3DFDAB61AD1A42, 0x9E13B115620A452D,
                0xD9B9CBFCC9EDBA19, 0x6A978742CA4AE576, 0xA14CB926613CF817, 0x1262F598629BA778, 0x55C88F71C97C584C,
                0xE6E6C3CFCADB0723, 0xDA9C7AA29EB3A624, 0x69B2361C9D14F94B, 0x2E184CF536F3067F, 0x9D36004B35545910,
                0x2B769F17CF112238, 0x9858D3A9CCB67D57, 0xDFF2A94067518263, 0x6CDCE5FE64F6DD0C, 0x50A65C93309E7C0B,
                0xE388102D33392364, 0xA4226AC498DEDC50, 0x170C267A9B79833F, 0xDCD7181E300F9E5E, 0x6FF954A033A8C131,
                0x28532E49984F3E05, 0x9B7D62F79BE8616A, 0xA707DB9ACF80C06D, 0x14299724CC279F02, 0x5383EDCD67C06036,
                0xE0ADA17364673F59
            },
            new ulong[]
            {
                0x0000000000000000, 0x54E979925CD0F10D, 0xA9D2F324B9A1E21A, 0xFD3B8AB6E5711317, 0xC17D4962DC4DDAB1,
                0x959430F0809D2BBC, 0x68AFBA4665EC38AB, 0x3C46C3D4393CC9A6, 0x10223DEE1795ABE7, 0x44CB447C4B455AEA,
                0xB9F0CECAAE3449FD, 0xED19B758F2E4B8F0, 0xD15F748CCBD87156, 0x85B60D1E9708805B, 0x788D87A87279934C,
                0x2C64FE3A2EA96241, 0x20447BDC2F2B57CE, 0x74AD024E73FBA6C3, 0x899688F8968AB5D4, 0xDD7FF16ACA5A44D9,
                0xE13932BEF3668D7F, 0xB5D04B2CAFB67C72, 0x48EBC19A4AC76F65, 0x1C02B80816179E68, 0x3066463238BEFC29,
                0x648F3FA0646E0D24, 0x99B4B516811F1E33, 0xCD5DCC84DDCFEF3E, 0xF11B0F50E4F32698, 0xA5F276C2B823D795,
                0x58C9FC745D52C482, 0x0C2085E60182358F, 0x4088F7B85E56AF9C, 0x14618E2A02865E91, 0xE95A049CE7F74D86,
                0xBDB37D0EBB27BC8B, 0x81F5BEDA821B752D, 0xD51CC748DECB8420, 0x28274DFE3BBA9737, 0x7CCE346C676A663A,
                0x50AACA5649C3047B, 0x0443B3C41513F576, 0xF9783972F062E661, 0xAD9140E0ACB2176C, 0x91D78334958EDECA,
                0xC53EFAA6C95E2FC7, 0x380570102C2F3CD0, 0x6CEC098270FFCDDD, 0x60CC8C64717DF852, 0x3425F5F62DAD095F,
                0xC91E7F40C8DC1A48, 0x9DF706D2940CEB45, 0xA1B1C506AD3022E3, 0xF558BC94F1E0D3EE, 0x086336221491C0F9,
                0x5C8A4FB0484131F4, 0x70EEB18A66E853B5, 0x2407C8183A38A2B8, 0xD93C42AEDF49B1AF, 0x8DD53B3C839940A2,
                0xB193F8E8BAA58904, 0xE57A817AE6757809, 0x18410BCC03046B1E, 0x4CA8725E5FD49A13, 0x8111EF70BCAD5F38,
                0xD5F896E2E07DAE35, 0x28C31C54050CBD22, 0x7C2A65C659DC4C2F, 0x406CA61260E08589, 0x1485DF803C307484,
                0xE9BE5536D9416793, 0xBD572CA48591969E, 0x9133D29EAB38F4DF, 0xC5DAAB0CF7E805D2, 0x38E121BA129916C5,
                0x6C0858284E49E7C8, 0x504E9BFC77752E6E, 0x04A7E26E2BA5DF63, 0xF99C68D8CED4CC74, 0xAD75114A92043D79,
                0xA15594AC938608F6, 0xF5BCED3ECF56F9FB, 0x088767882A27EAEC, 0x5C6E1E1A76F71BE1, 0x6028DDCE4FCBD247,
                0x34C1A45C131B234A, 0xC9FA2EEAF66A305D, 0x9D135778AABAC150, 0xB177A9428413A311, 0xE59ED0D0D8C3521C,
                0x18A55A663DB2410B, 0x4C4C23F46162B006, 0x700AE020585E79A0, 0x24E399B2048E88AD, 0xD9D81304E1FF9BBA,
                0x8D316A96BD2F6AB7, 0xC19918C8E2FBF0A4, 0x9570615ABE2B01A9, 0x684BEBEC5B5A12BE, 0x3CA2927E078AE3B3,
                0x00E451AA3EB62A15, 0x540D28386266DB18, 0xA936A28E8717C80F, 0xFDDFDB1CDBC73902, 0xD1BB2526F56E5B43,
                0x85525CB4A9BEAA4E, 0x7869D6024CCFB959, 0x2C80AF90101F4854, 0x10C66C44292381F2, 0x442F15D675F370FF,
                0xB9149F60908263E8, 0xEDFDE6F2CC5292E5, 0xE1DD6314CDD0A76A, 0xB5341A8691005667, 0x480F903074714570,
                0x1CE6E9A228A1B47D, 0x20A02A76119D7DDB, 0x744953E44D4D8CD6, 0x8972D952A83C9FC1, 0xDD9BA0C0F4EC6ECC,
                0xF1FF5EFADA450C8D, 0xA51627688695FD80, 0x582DADDE63E4EE97, 0x0CC4D44C3F341F9A, 0x308217980608D63C,
                0x646B6E0A5AD82731, 0x9950E4BCBFA93426, 0xCDB99D2EE379C52B, 0x90FB71CAD654A0F5, 0xC41208588A8451F8,
                0x392982EE6FF542EF, 0x6DC0FB7C3325B3E2, 0x518638A80A197A44, 0x056F413A56C98B49, 0xF854CB8CB3B8985E,
                0xACBDB21EEF686953, 0x80D94C24C1C10B12, 0xD43035B69D11FA1F, 0x290BBF007860E908, 0x7DE2C69224B01805,
                0x41A405461D8CD1A3, 0x154D7CD4415C20AE, 0xE876F662A42D33B9, 0xBC9F8FF0F8FDC2B4, 0xB0BF0A16F97FF73B,
                0xE4567384A5AF0636, 0x196DF93240DE1521, 0x4D8480A01C0EE42C, 0x71C2437425322D8A, 0x252B3AE679E2DC87,
                0xD810B0509C93CF90, 0x8CF9C9C2C0433E9D, 0xA09D37F8EEEA5CDC, 0xF4744E6AB23AADD1, 0x094FC4DC574BBEC6,
                0x5DA6BD4E0B9B4FCB, 0x61E07E9A32A7866D, 0x350907086E777760, 0xC8328DBE8B066477, 0x9CDBF42CD7D6957A,
                0xD073867288020F69, 0x849AFFE0D4D2FE64, 0x79A1755631A3ED73, 0x2D480CC46D731C7E, 0x110ECF10544FD5D8,
                0x45E7B682089F24D5, 0xB8DC3C34EDEE37C2, 0xEC3545A6B13EC6CF, 0xC051BB9C9F97A48E, 0x94B8C20EC3475583,
                0x698348B826364694, 0x3D6A312A7AE6B799, 0x012CF2FE43DA7E3F, 0x55C58B6C1F0A8F32, 0xA8FE01DAFA7B9C25,
                0xFC177848A6AB6D28, 0xF037FDAEA72958A7, 0xA4DE843CFBF9A9AA, 0x59E50E8A1E88BABD, 0x0D0C771842584BB0,
                0x314AB4CC7B648216, 0x65A3CD5E27B4731B, 0x989847E8C2C5600C, 0xCC713E7A9E159101, 0xE015C040B0BCF340,
                0xB4FCB9D2EC6C024D, 0x49C73364091D115A, 0x1D2E4AF655CDE057, 0x216889226CF129F1, 0x7581F0B03021D8FC,
                0x88BA7A06D550CBEB, 0xDC53039489803AE6, 0x11EA9EBA6AF9FFCD, 0x4503E72836290EC0, 0xB8386D9ED3581DD7,
                0xECD1140C8F88ECDA, 0xD097D7D8B6B4257C, 0x847EAE4AEA64D471, 0x794524FC0F15C766, 0x2DAC5D6E53C5366B,
                0x01C8A3547D6C542A, 0x5521DAC621BCA527, 0xA81A5070C4CDB630, 0xFCF329E2981D473D, 0xC0B5EA36A1218E9B,
                0x945C93A4FDF17F96, 0x6967191218806C81, 0x3D8E608044509D8C, 0x31AEE56645D2A803, 0x65479CF41902590E,
                0x987C1642FC734A19, 0xCC956FD0A0A3BB14, 0xF0D3AC04999F72B2, 0xA43AD596C54F83BF, 0x59015F20203E90A8,
                0x0DE826B27CEE61A5, 0x218CD888524703E4, 0x7565A11A0E97F2E9, 0x885E2BACEBE6E1FE, 0xDCB7523EB73610F3,
                0xE0F191EA8E0AD955, 0xB418E878D2DA2858, 0x492362CE37AB3B4F, 0x1DCA1B5C6B7BCA42, 0x5162690234AF5051,
                0x058B1090687FA15C, 0xF8B09A268D0EB24B, 0xAC59E3B4D1DE4346, 0x901F2060E8E28AE0, 0xC4F659F2B4327BED,
                0x39CDD344514368FA, 0x6D24AAD60D9399F7, 0x414054EC233AFBB6, 0x15A92D7E7FEA0ABB, 0xE892A7C89A9B19AC,
                0xBC7BDE5AC64BE8A1, 0x803D1D8EFF772107, 0xD4D4641CA3A7D00A, 0x29EFEEAA46D6C31D, 0x7D0697381A063210,
                0x712612DE1B84079F, 0x25CF6B4C4754F692, 0xD8F4E1FAA225E585, 0x8C1D9868FEF51488, 0xB05B5BBCC7C9DD2E,
                0xE4B2222E9B192C23, 0x1989A8987E683F34, 0x4D60D10A22B8CE39, 0x61042F300C11AC78, 0x35ED56A250C15D75,
                0xC8D6DC14B5B04E62, 0x9C3FA586E960BF6F, 0xA0796652D05C76C9, 0xF4901FC08C8C87C4, 0x09AB957669FD94D3,
                0x5D42ECE4352D65DE
            },
            new ulong[]
            {
                0x0000000000000000, 0x3F0BE14A916A6DCB, 0x7E17C29522D4DB96, 0x411C23DFB3BEB65D, 0xFC2F852A45A9B72C,
                0xC3246460D4C3DAE7, 0x823847BF677D6CBA, 0xBD33A6F5F6170171, 0x6A87A57F245D70DD, 0x558C4435B5371D16,
                0x149067EA0689AB4B, 0x2B9B86A097E3C680, 0x96A8205561F4C7F1, 0xA9A3C11FF09EAA3A, 0xE8BFE2C043201C67,
                0xD7B4038AD24A71AC, 0xD50F4AFE48BAE1BA, 0xEA04ABB4D9D08C71, 0xAB18886B6A6E3A2C, 0x94136921FB0457E7,
                0x2920CFD40D135696, 0x162B2E9E9C793B5D, 0x57370D412FC78D00, 0x683CEC0BBEADE0CB, 0xBF88EF816CE79167,
                0x80830ECBFD8DFCAC, 0xC19F2D144E334AF1, 0xFE94CC5EDF59273A, 0x43A76AAB294E264B, 0x7CAC8BE1B8244B80,
                0x3DB0A83E0B9AFDDD, 0x02BB49749AF09016, 0x38C63AD73E7BDDF1, 0x07CDDB9DAF11B03A, 0x46D1F8421CAF0667,
                0x79DA19088DC56BAC, 0xC4E9BFFD7BD26ADD, 0xFBE25EB7EAB80716, 0xBAFE7D685906B14B, 0x85F59C22C86CDC80,
                0x52419FA81A26AD2C, 0x6D4A7EE28B4CC0E7, 0x2C565D3D38F276BA, 0x135DBC77A9981B71, 0xAE6E1A825F8F1A00,
                0x9165FBC8CEE577CB, 0xD079D8177D5BC196, 0xEF72395DEC31AC5D, 0xEDC9702976C13C4B, 0xD2C29163E7AB5180,
                0x93DEB2BC5415E7DD, 0xACD553F6C57F8A16, 0x11E6F50333688B67, 0x2EED1449A202E6AC, 0x6FF1379611BC50F1,
                0x50FAD6DC80D63D3A, 0x874ED556529C4C96, 0xB845341CC3F6215D, 0xF95917C370489700, 0xC652F689E122FACB,
                0x7B61507C1735FBBA, 0x446AB136865F9671, 0x057692E935E1202C, 0x3A7D73A3A48B4DE7, 0x718C75AE7CF7BBE2,
                0x4E8794E4ED9DD629, 0x0F9BB73B5E236074, 0x30905671CF490DBF, 0x8DA3F084395E0CCE, 0xB2A811CEA8346105,
                0xF3B432111B8AD758, 0xCCBFD35B8AE0BA93, 0x1B0BD0D158AACB3F, 0x2400319BC9C0A6F4, 0x651C12447A7E10A9,
                0x5A17F30EEB147D62, 0xE72455FB1D037C13, 0xD82FB4B18C6911D8, 0x9933976E3FD7A785, 0xA6387624AEBDCA4E,
                0xA4833F50344D5A58, 0x9B88DE1AA5273793, 0xDA94FDC5169981CE, 0xE59F1C8F87F3EC05, 0x58ACBA7A71E4ED74,
                0x67A75B30E08E80BF, 0x26BB78EF533036E2, 0x19B099A5C25A5B29, 0xCE049A2F10102A85, 0xF10F7B65817A474E,
                0xB01358BA32C4F113, 0x8F18B9F0A3AE9CD8, 0x322B1F0555B99DA9, 0x0D20FE4FC4D3F062, 0x4C3CDD90776D463F,
                0x73373CDAE6072BF4, 0x494A4F79428C6613, 0x7641AE33D3E60BD8, 0x375D8DEC6058BD85, 0x08566CA6F132D04E,
                0xB565CA530725D13F, 0x8A6E2B19964FBCF4, 0xCB7208C625F10AA9, 0xF479E98CB49B6762, 0x23CDEA0666D116CE,
                0x1CC60B4CF7BB7B05, 0x5DDA28934405CD58, 0x62D1C9D9D56FA093, 0xDFE26F2C2378A1E2, 0xE0E98E66B212CC29,
                0xA1F5ADB901AC7A74, 0x9EFE4CF390C617BF, 0x9C4505870A3687A9, 0xA34EE4CD9B5CEA62, 0xE252C71228E25C3F,
                0xDD592658B98831F4, 0x606A80AD4F9F3085, 0x5F6161E7DEF55D4E, 0x1E7D42386D4BEB13, 0x2176A372FC2186D8,
                0xF6C2A0F82E6BF774, 0xC9C941B2BF019ABF, 0x88D5626D0CBF2CE2, 0xB7DE83279DD54129, 0x0AED25D26BC24058,
                0x35E6C498FAA82D93, 0x74FAE74749169BCE, 0x4BF1060DD87CF605, 0xE318EB5CF9EF77C4, 0xDC130A1668851A0F,
                0x9D0F29C9DB3BAC52, 0xA204C8834A51C199, 0x1F376E76BC46C0E8, 0x203C8F3C2D2CAD23, 0x6120ACE39E921B7E,
                0x5E2B4DA90FF876B5, 0x899F4E23DDB20719, 0xB694AF694CD86AD2, 0xF7888CB6FF66DC8F, 0xC8836DFC6E0CB144,
                0x75B0CB09981BB035, 0x4ABB2A430971DDFE, 0x0BA7099CBACF6BA3, 0x34ACE8D62BA50668, 0x3617A1A2B155967E,
                0x091C40E8203FFBB5, 0x4800633793814DE8, 0x770B827D02EB2023, 0xCA382488F4FC2152, 0xF533C5C265964C99,
                0xB42FE61DD628FAC4, 0x8B2407574742970F, 0x5C9004DD9508E6A3, 0x639BE59704628B68, 0x2287C648B7DC3D35,
                0x1D8C270226B650FE, 0xA0BF81F7D0A1518F, 0x9FB460BD41CB3C44, 0xDEA84362F2758A19, 0xE1A3A228631FE7D2,
                0xDBDED18BC794AA35, 0xE4D530C156FEC7FE, 0xA5C9131EE54071A3, 0x9AC2F254742A1C68, 0x27F154A1823D1D19,
                0x18FAB5EB135770D2, 0x59E69634A0E9C68F, 0x66ED777E3183AB44, 0xB15974F4E3C9DAE8, 0x8E5295BE72A3B723,
                0xCF4EB661C11D017E, 0xF045572B50776CB5, 0x4D76F1DEA6606DC4, 0x727D1094370A000F, 0x3361334B84B4B652,
                0x0C6AD20115DEDB99, 0x0ED19B758F2E4B8F, 0x31DA7A3F1E442644, 0x70C659E0ADFA9019, 0x4FCDB8AA3C90FDD2,
                0xF2FE1E5FCA87FCA3, 0xCDF5FF155BED9168, 0x8CE9DCCAE8532735, 0xB3E23D8079394AFE, 0x64563E0AAB733B52,
                0x5B5DDF403A195699, 0x1A41FC9F89A7E0C4, 0x254A1DD518CD8D0F, 0x9879BB20EEDA8C7E, 0xA7725A6A7FB0E1B5,
                0xE66E79B5CC0E57E8, 0xD96598FF5D643A23, 0x92949EF28518CC26, 0xAD9F7FB81472A1ED, 0xEC835C67A7CC17B0,
                0xD388BD2D36A67A7B, 0x6EBB1BD8C0B17B0A, 0x51B0FA9251DB16C1, 0x10ACD94DE265A09C, 0x2FA73807730FCD57,
                0xF8133B8DA145BCFB, 0xC718DAC7302FD130, 0x8604F9188391676D, 0xB90F185212FB0AA6, 0x043CBEA7E4EC0BD7,
                0x3B375FED7586661C, 0x7A2B7C32C638D041, 0x45209D785752BD8A, 0x479BD40CCDA22D9C, 0x789035465CC84057,
                0x398C1699EF76F60A, 0x0687F7D37E1C9BC1, 0xBBB45126880B9AB0, 0x84BFB06C1961F77B, 0xC5A393B3AADF4126,
                0xFAA872F93BB52CED, 0x2D1C7173E9FF5D41, 0x121790397895308A, 0x530BB3E6CB2B86D7, 0x6C0052AC5A41EB1C,
                0xD133F459AC56EA6D, 0xEE3815133D3C87A6, 0xAF2436CC8E8231FB, 0x902FD7861FE85C30, 0xAA52A425BB6311D7,
                0x9559456F2A097C1C, 0xD44566B099B7CA41, 0xEB4E87FA08DDA78A, 0x567D210FFECAA6FB, 0x6976C0456FA0CB30,
                0x286AE39ADC1E7D6D, 0x176102D04D7410A6, 0xC0D5015A9F3E610A, 0xFFDEE0100E540CC1, 0xBEC2C3CFBDEABA9C,
                0x81C922852C80D757, 0x3CFA8470DA97D626, 0x03F1653A4BFDBBED, 0x42ED46E5F8430DB0, 0x7DE6A7AF6929607B,
                0x7F5DEEDBF3D9F06D, 0x40560F9162B39DA6, 0x014A2C4ED10D2BFB, 0x3E41CD0440674630, 0x83726BF1B6704741,
                0xBC798ABB271A2A8A, 0xFD65A96494A49CD7, 0xC26E482E05CEF11C, 0x15DA4BA4D78480B0, 0x2AD1AAEE46EEED7B,
                0x6BCD8931F5505B26, 0x54C6687B643A36ED, 0xE9F5CE8E922D379C, 0xD6FE2FC403475A57, 0x97E20C1BB0F9EC0A,
                0xA8E9ED51219381C1
            },
            new ulong[]
            {
                0x0000000000000000, 0x1DEE8A5E222CA1DC, 0x3BDD14BC445943B8, 0x26339EE26675E264, 0x77BA297888B28770,
                0x6A54A326AA9E26AC, 0x4C673DC4CCEBC4C8, 0x5189B79AEEC76514, 0xEF7452F111650EE0, 0xF29AD8AF3349AF3C,
                0xD4A9464D553C4D58, 0xC947CC137710EC84, 0x98CE7B8999D78990, 0x8520F1D7BBFB284C, 0xA3136F35DD8ECA28,
                0xBEFDE56BFFA26BF4, 0x4C300AC98DC40345, 0x51DE8097AFE8A299, 0x77ED1E75C99D40FD, 0x6A03942BEBB1E121,
                0x3B8A23B105768435, 0x2664A9EF275A25E9, 0x0057370D412FC78D, 0x1DB9BD5363036651, 0xA34458389CA10DA5,
                0xBEAAD266BE8DAC79, 0x98994C84D8F84E1D, 0x8577C6DAFAD4EFC1, 0xD4FE714014138AD5, 0xC910FB1E363F2B09,
                0xEF2365FC504AC96D, 0xF2CDEFA2726668B1, 0x986015931B88068A, 0x858E9FCD39A4A756, 0xA3BD012F5FD14532,
                0xBE538B717DFDE4EE, 0xEFDA3CEB933A81FA, 0xF234B6B5B1162026, 0xD4072857D763C242, 0xC9E9A209F54F639E,
                0x771447620AED086A, 0x6AFACD3C28C1A9B6, 0x4CC953DE4EB44BD2, 0x5127D9806C98EA0E, 0x00AE6E1A825F8F1A,
                0x1D40E444A0732EC6, 0x3B737AA6C606CCA2, 0x269DF0F8E42A6D7E, 0xD4501F5A964C05CF, 0xC9BE9504B460A413,
                0xEF8D0BE6D2154677, 0xF26381B8F039E7AB, 0xA3EA36221EFE82BF, 0xBE04BC7C3CD22363, 0x9837229E5AA7C107,
                0x85D9A8C0788B60DB, 0x3B244DAB87290B2F, 0x26CAC7F5A505AAF3, 0x00F95917C3704897, 0x1D17D349E15CE94B,
                0x4C9E64D30F9B8C5F, 0x5170EE8D2DB72D83, 0x7743706F4BC2CFE7, 0x6AADFA3169EE6E3B, 0xA218840D981E1391,
                0xBFF60E53BA32B24D, 0x99C590B1DC475029, 0x842B1AEFFE6BF1F5, 0xD5A2AD7510AC94E1, 0xC84C272B3280353D,
                0xEE7FB9C954F5D759, 0xF391339776D97685, 0x4D6CD6FC897B1D71, 0x50825CA2AB57BCAD, 0x76B1C240CD225EC9,
                0x6B5F481EEF0EFF15, 0x3AD6FF8401C99A01, 0x273875DA23E53BDD, 0x010BEB384590D9B9, 0x1CE5616667BC7865,
                0xEE288EC415DA10D4, 0xF3C6049A37F6B108, 0xD5F59A785183536C, 0xC81B102673AFF2B0, 0x9992A7BC9D6897A4,
                0x847C2DE2BF443678, 0xA24FB300D931D41C, 0xBFA1395EFB1D75C0, 0x015CDC3504BF1E34, 0x1CB2566B2693BFE8,
                0x3A81C88940E65D8C, 0x276F42D762CAFC50, 0x76E6F54D8C0D9944, 0x6B087F13AE213898, 0x4D3BE1F1C854DAFC,
                0x50D56BAFEA787B20, 0x3A78919E8396151B, 0x27961BC0A1BAB4C7, 0x01A58522C7CF56A3, 0x1C4B0F7CE5E3F77F,
                0x4DC2B8E60B24926B, 0x502C32B8290833B7, 0x761FAC5A4F7DD1D3, 0x6BF126046D51700F, 0xD50CC36F92F31BFB,
                0xC8E24931B0DFBA27, 0xEED1D7D3D6AA5843, 0xF33F5D8DF486F99F, 0xA2B6EA171A419C8B, 0xBF586049386D3D57,
                0x996BFEAB5E18DF33, 0x848574F57C347EEF, 0x76489B570E52165E, 0x6BA611092C7EB782, 0x4D958FEB4A0B55E6,
                0x507B05B56827F43A, 0x01F2B22F86E0912E, 0x1C1C3871A4CC30F2, 0x3A2FA693C2B9D296, 0x27C12CCDE095734A,
                0x993CC9A61F3718BE, 0x84D243F83D1BB962, 0xA2E1DD1A5B6E5B06, 0xBF0F57447942FADA, 0xEE86E0DE97859FCE,
                0xF3686A80B5A93E12, 0xD55BF462D3DCDC76, 0xC8B57E3CF1F07DAA, 0xD6E9A7309F3239A7, 0xCB072D6EBD1E987B,
                0xED34B38CDB6B7A1F, 0xF0DA39D2F947DBC3, 0xA1538E481780BED7, 0xBCBD041635AC1F0B, 0x9A8E9AF453D9FD6F,
                0x876010AA71F55CB3, 0x399DF5C18E573747, 0x24737F9FAC7B969B, 0x0240E17DCA0E74FF, 0x1FAE6B23E822D523,
                0x4E27DCB906E5B037, 0x53C956E724C911EB, 0x75FAC80542BCF38F, 0x6814425B60905253, 0x9AD9ADF912F63AE2,
                0x873727A730DA9B3E, 0xA104B94556AF795A, 0xBCEA331B7483D886, 0xED6384819A44BD92, 0xF08D0EDFB8681C4E,
                0xD6BE903DDE1DFE2A, 0xCB501A63FC315FF6, 0x75ADFF0803933402, 0x6843755621BF95DE, 0x4E70EBB447CA77BA,
                0x539E61EA65E6D666, 0x0217D6708B21B372, 0x1FF95C2EA90D12AE, 0x39CAC2CCCF78F0CA, 0x24244892ED545116,
                0x4E89B2A384BA3F2D, 0x536738FDA6969EF1, 0x7554A61FC0E37C95, 0x68BA2C41E2CFDD49, 0x39339BDB0C08B85D,
                0x24DD11852E241981, 0x02EE8F674851FBE5, 0x1F0005396A7D5A39, 0xA1FDE05295DF31CD, 0xBC136A0CB7F39011,
                0x9A20F4EED1867275, 0x87CE7EB0F3AAD3A9, 0xD647C92A1D6DB6BD, 0xCBA943743F411761, 0xED9ADD965934F505,
                0xF07457C87B1854D9, 0x02B9B86A097E3C68, 0x1F5732342B529DB4, 0x3964ACD64D277FD0, 0x248A26886F0BDE0C,
                0x7503911281CCBB18, 0x68ED1B4CA3E01AC4, 0x4EDE85AEC595F8A0, 0x53300FF0E7B9597C, 0xEDCDEA9B181B3288,
                0xF02360C53A379354, 0xD610FE275C427130, 0xCBFE74797E6ED0EC, 0x9A77C3E390A9B5F8, 0x879949BDB2851424,
                0xA1AAD75FD4F0F640, 0xBC445D01F6DC579C, 0x74F1233D072C2A36, 0x691FA96325008BEA, 0x4F2C37814375698E,
                0x52C2BDDF6159C852, 0x034B0A458F9EAD46, 0x1EA5801BADB20C9A, 0x38961EF9CBC7EEFE, 0x257894A7E9EB4F22,
                0x9B8571CC164924D6, 0x866BFB923465850A, 0xA05865705210676E, 0xBDB6EF2E703CC6B2, 0xEC3F58B49EFBA3A6,
                0xF1D1D2EABCD7027A, 0xD7E24C08DAA2E01E, 0xCA0CC656F88E41C2, 0x38C129F48AE82973, 0x252FA3AAA8C488AF,
                0x031C3D48CEB16ACB, 0x1EF2B716EC9DCB17, 0x4F7B008C025AAE03, 0x52958AD220760FDF, 0x74A614304603EDBB,
                0x69489E6E642F4C67, 0xD7B57B059B8D2793, 0xCA5BF15BB9A1864F, 0xEC686FB9DFD4642B, 0xF186E5E7FDF8C5F7,
                0xA00F527D133FA0E3, 0xBDE1D8233113013F, 0x9BD246C15766E35B, 0x863CCC9F754A4287, 0xEC9136AE1CA42CBC,
                0xF17FBCF03E888D60, 0xD74C221258FD6F04, 0xCAA2A84C7AD1CED8, 0x9B2B1FD69416ABCC, 0x86C59588B63A0A10,
                0xA0F60B6AD04FE874, 0xBD188134F26349A8, 0x03E5645F0DC1225C, 0x1E0BEE012FED8380, 0x383870E3499861E4,
                0x25D6FABD6BB4C038, 0x745F4D278573A52C, 0x69B1C779A75F04F0, 0x4F82599BC12AE694, 0x526CD3C5E3064748,
                0xA0A13C6791602FF9, 0xBD4FB639B34C8E25, 0x9B7C28DBD5396C41, 0x8692A285F715CD9D, 0xD71B151F19D2A889,
                0xCAF59F413BFE0955, 0xECC601A35D8BEB31, 0xF1288BFD7FA74AED, 0x4FD56E9680052119, 0x523BE4C8A22980C5,
                0x74087A2AC45C62A1, 0x69E6F074E670C37D, 0x386F47EE08B7A669, 0x2581CDB02A9B07B5, 0x03B253524CEEE5D1,
                0x1E5CD90C6EC2440D
            }
        };

        readonly ulong     _finalSeed;
        readonly IntPtr    _nativeContext;
        readonly ulong[][] _table;
        readonly bool      _useEcma;
        readonly bool      _useNative;
        ulong              _hashInt;

        /// <summary>Initializes the CRC64 table and seed as CRC64-ECMA</summary>
        public Crc64Context()
        {
            _hashInt   = CRC64_ECMA_SEED;
            _table     = _ecmaCrc64Table;
            _finalSeed = CRC64_ECMA_SEED;
            _useEcma   = true;

            if(!Native.IsSupported)
                return;

            _nativeContext = crc64_init();
            _useNative     = _nativeContext != IntPtr.Zero;
        }

        /// <summary>Initializes the CRC16 table with a custom polynomial and seed</summary>
        public Crc64Context(ulong polynomial, ulong seed)
        {
            _hashInt   = seed;
            _finalSeed = seed;
            _useEcma   = polynomial == CRC64_ECMA_POLY && seed == CRC64_ECMA_SEED;

            if(Native.IsSupported && _useEcma)
            {
                _nativeContext = crc64_init();
                _useNative     = _nativeContext != IntPtr.Zero;
            }
            else
                _table = GenerateTable(polynomial);
        }

        /// <inheritdoc />
        /// <summary>Updates the hash with data.</summary>
        /// <param name="data">Data buffer.</param>
        /// <param name="len">Length of buffer to hash.</param>
        public void Update(byte[] data, uint len) =>
            Step(ref _hashInt, _table, data, len, _useEcma, _useNative, _nativeContext);

        /// <inheritdoc />
        /// <summary>Updates the hash with data.</summary>
        /// <param name="data">Data buffer.</param>
        public void Update(byte[] data) => Update(data, (uint)data.Length);

        /// <inheritdoc />
        /// <summary>Returns a byte array of the hash value.</summary>
        public byte[] Final()
        {
            ulong crc = _hashInt ^ _finalSeed;

            if(!_useNative ||
               !_useEcma)
                return BigEndianBitConverter.GetBytes(crc);

            crc64_final(_nativeContext, ref crc);
            crc64_free(_nativeContext);

            return BigEndianBitConverter.GetBytes(crc);
        }

        /// <inheritdoc />
        /// <summary>Returns a hexadecimal representation of the hash value.</summary>
        public string End()
        {
            ulong crc = _hashInt ^ _finalSeed;

            var crc64Output = new StringBuilder();

            if(_useNative && _useEcma)
            {
                crc64_final(_nativeContext, ref crc);
                crc64_free(_nativeContext);
            }

            for(int i = 0; i < BigEndianBitConverter.GetBytes(crc).Length; i++)
                crc64Output.Append(BigEndianBitConverter.GetBytes(crc)[i].ToString("x2"));

            return crc64Output.ToString();
        }

        [DllImport("libAaru.Checksums.Native", SetLastError = true)]
        static extern IntPtr crc64_init();

        [DllImport("libAaru.Checksums.Native", SetLastError = true)]
        static extern int crc64_update(IntPtr ctx, byte[] data, uint len);

        [DllImport("libAaru.Checksums.Native", SetLastError = true)]
        static extern int crc64_final(IntPtr ctx, ref ulong crc);

        [DllImport("libAaru.Checksums.Native", SetLastError = true)]
        static extern void crc64_free(IntPtr ctx);

        static ulong[][] GenerateTable(ulong polynomial)
        {
            ulong[][] table = new ulong[8][];

            for(int i = 0; i < 8; i++)
                table[i] = new ulong[256];

            for(int i = 0; i < 256; i++)
            {
                ulong entry = (ulong)i;

                for(int j = 0; j < 8; j++)
                    if((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry >>= 1;

                table[0][i] = entry;
            }

            for(int slice = 1; slice < 4; slice++)
                for(int i = 0; i < 256; i++)
                    table[slice][i] = (table[slice - 1][i] >> 8) ^ table[0][table[slice - 1][i] & 0xFF];

            return table;
        }

        static void Step(ref ulong previousCrc, ulong[][] table, byte[] data, uint len, bool useEcma, bool useNative,
                         IntPtr nativeContext)
        {
            if(useNative && useEcma)
            {
                crc64_update(nativeContext, data, len);

                return;
            }

            int dataOff = 0;

            if(useEcma               &&
               Pclmulqdq.IsSupported &&
               Sse41.IsSupported     &&
               Ssse3.IsSupported     &&
               Sse2.IsSupported)
            {
                // Only works in blocks of 32 bytes
                uint blocks = len / 32;

                if(blocks > 0)
                {
                    previousCrc = ~CRC64.Clmul.Step(~previousCrc, data, blocks * 32);

                    dataOff =  (int)(blocks * 32);
                    len     -= blocks * 32;
                }

                if(len == 0)
                    return;
            }

            // Unroll according to Intel slicing by uint8_t
            // http://www.intel.com/technology/comms/perfnet/download/CRC_generators.pdf
            // http://sourceforge.net/projects/slicing-by-8/

            ulong crc = previousCrc;

            if(len > 4)
            {
                long limit = dataOff + (len & ~(uint)3);
                len &= 3;

                while(dataOff < limit)
                {
                    uint tmp = (uint)(crc ^ BitConverter.ToUInt32(data, dataOff));
                    dataOff += 4;

                    crc = table[3][tmp & 0xFF] ^ table[2][(tmp >> 8)  & 0xFF] ^ (crc >> 32) ^
                          table[1][(tmp                        >> 16) & 0xFF] ^ table[0][tmp >> 24];
                }
            }

            while(len-- != 0)
                crc = table[0][data[dataOff++] ^ (crc & 0xFF)] ^ (crc >> 8);

            previousCrc = crc;
        }

        /// <summary>Gets the hash of a file</summary>
        /// <param name="filename">File path.</param>
        public static byte[] File(string filename)
        {
            File(filename, out byte[] localHash);

            return localHash;
        }

        /// <summary>Gets the hash of a file in hexadecimal and as a byte array.</summary>
        /// <param name="filename">File path.</param>
        /// <param name="hash">Byte array of the hash value.</param>
        public static string File(string filename, out byte[] hash) =>
            File(filename, out hash, CRC64_ECMA_POLY, CRC64_ECMA_SEED);

        /// <summary>Gets the hash of a file in hexadecimal and as a byte array.</summary>
        /// <param name="filename">File path.</param>
        /// <param name="hash">Byte array of the hash value.</param>
        /// <param name="polynomial">CRC polynomial</param>
        /// <param name="seed">CRC seed</param>
        public static string File(string filename, out byte[] hash, ulong polynomial, ulong seed)
        {
            bool   useEcma       = polynomial == CRC64_ECMA_POLY && seed == CRC64_ECMA_SEED;
            bool   useNative     = Native.IsSupported;
            IntPtr nativeContext = IntPtr.Zero;

            if(useNative && useEcma)
            {
                nativeContext = crc64_init();
                useNative     = nativeContext != IntPtr.Zero;
            }

            var fileStream = new FileStream(filename, FileMode.Open);

            ulong localHashInt = seed;

            ulong[][] localTable = GenerateTable(polynomial);

            byte[] buffer = new byte[65536];
            int    read   = fileStream.Read(buffer, 0, 65536);

            while(read > 0)
            {
                Step(ref localHashInt, localTable, buffer, (uint)read, useEcma, useNative, nativeContext);

                read = fileStream.Read(buffer, 0, 65536);
            }

            localHashInt ^= seed;

            if(useNative && useEcma)
            {
                crc64_final(nativeContext, ref localHashInt);
                crc64_free(nativeContext);
            }

            hash = BigEndianBitConverter.GetBytes(localHashInt);

            var crc64Output = new StringBuilder();

            foreach(byte h in hash)
                crc64Output.Append(h.ToString("x2"));

            fileStream.Close();

            return crc64Output.ToString();
        }

        /// <summary>Gets the hash of the specified data buffer.</summary>
        /// <param name="data">Data buffer.</param>
        /// <param name="len">Length of the data buffer to hash.</param>
        /// <param name="hash">Byte array of the hash value.</param>
        public static string Data(byte[] data, uint len, out byte[] hash) =>
            Data(data, len, out hash, CRC64_ECMA_POLY, CRC64_ECMA_SEED);

        /// <summary>Gets the hash of the specified data buffer.</summary>
        /// <param name="data">Data buffer.</param>
        /// <param name="len">Length of the data buffer to hash.</param>
        /// <param name="hash">Byte array of the hash value.</param>
        /// <param name="polynomial">CRC polynomial</param>
        /// <param name="seed">CRC seed</param>
        public static string Data(byte[] data, uint len, out byte[] hash, ulong polynomial, ulong seed)
        {
            bool   useEcma       = polynomial == CRC64_ECMA_POLY && seed == CRC64_ECMA_SEED;
            bool   useNative     = Native.IsSupported;
            IntPtr nativeContext = IntPtr.Zero;

            if(useNative && useEcma)
            {
                nativeContext = crc64_init();
                useNative     = nativeContext != IntPtr.Zero;
            }

            ulong localHashInt = seed;

            ulong[][] localTable = GenerateTable(polynomial);

            Step(ref localHashInt, localTable, data, len, useEcma, useNative, nativeContext);

            localHashInt ^= seed;

            if(useNative && useEcma)
            {
                crc64_final(nativeContext, ref localHashInt);
                crc64_free(nativeContext);
            }

            hash = BigEndianBitConverter.GetBytes(localHashInt);

            var crc64Output = new StringBuilder();

            foreach(byte h in hash)
                crc64Output.Append(h.ToString("x2"));

            return crc64Output.ToString();
        }

        /// <summary>Gets the hash of the specified data buffer.</summary>
        /// <param name="data">Data buffer.</param>
        /// <param name="hash">Byte array of the hash value.</param>
        public static string Data(byte[] data, out byte[] hash) => Data(data, (uint)data.Length, out hash);
    }
}