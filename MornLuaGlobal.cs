using MornGlobal;
using UnityEngine;

namespace MornLua
{
    [CreateAssetMenu(fileName = nameof(MornLuaGlobal), menuName = "Morn/" + nameof(MornLuaGlobal))]
    public sealed class MornLuaGlobal : MornGlobalBase<MornLuaGlobal>
    {
        protected override string ModuleName => nameof(MornLua);
    }
}