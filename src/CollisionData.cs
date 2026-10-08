using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

internal sealed class Box
{
    internal int Group;
    internal float Left,Bottom,Width,Height;
    internal int Player {get{return Group/5+1;}}
    internal int ExtraKind=-1;
    internal int Kind {get{return ExtraKind>=0?ExtraKind:Group%5;}}
}

