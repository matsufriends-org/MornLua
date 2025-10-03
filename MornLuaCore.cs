using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lua;
using Lua.Standard;
using Lua.Unity;

namespace MornLua
{
    public class MornLuaCore
    {
        private readonly Dictionary<string, LuaFunction> _functions = new();
        private readonly List<string> _requiredModules = new();
        private readonly ILuaModuleLoader _moduleLoader;

        public MornLuaCore(ILuaModuleLoader moduleLoader = null)
        {
            _moduleLoader = moduleLoader;
            SetUpDefaultFunctions();
        }

        private void SetUpDefaultFunctions()
        {
            AddDefaultFunction(
                "print",
                new LuaFunction((context, buffer, _) =>
                {
                    var log = ContextToLog(context);
                    MornLuaLogger.Log($"print: {log}");
                    buffer.Span[0] = log;
                    return new(1);
                }));
            AddDefaultFunction(
                "warn",
                new LuaFunction((context, buffer, _) =>
                {
                    var log = ContextToLog(context);
                    MornLuaLogger.LogError($"warn: {log}");
                    buffer.Span[0] = log;
                    return new(1);
                }));
            AddDefaultFunction(
                "error",
                new LuaFunction((context, buffer, _) =>
                {
                    var log = ContextToLog(context);
                    MornLuaLogger.LogError($"error: {log}");
                    buffer.Span[0] = log;
                    return new(1);
                }));
            AddDefaultFunction(
                "wait",
                new LuaFunction(async (context, _, ct) =>
                {
                    if (context.Arguments.Length < 1)
                    {
                        MornLuaLogger.LogWarning("wait 関数には少なくとも1つの引数が必要です。");
                        return 0;
                    }

                    var seconds = context.GetArgument<float>(0);
                    await UniTask.Delay(TimeSpan.FromDays(seconds), cancellationToken: ct);
                    return 0;
                }));
            AddDefaultFunction(
                "coroutine",
                new LuaFunction(async (context, _, ct) =>
                {
                    if (context.ArgumentCount != 1)
                    {
                        MornLuaLogger.LogWarning("NoWait 関数には1つの引数が必要です。");
                        return 0;
                    }

                    var function = context.GetArgument<LuaFunction>(0);
                    var newState = LuaState.Create();
                    function.InvokeAsync(newState, Array.Empty<LuaValue>(), ct).AsUniTask().Forget();
                    return 0;
                }));
        }

        private string ContextToLog(LuaFunctionExecutionContext context)
        {
            var sb = new StringBuilder();
            foreach (var value in context.Arguments)
            {
                if (sb.Length > 0)
                {
                    sb.Append("\t");
                }

                sb.Append(value.ToString());
            }

            return sb.ToString();
        }

        public void AddDefaultFunction(string functionName, LuaFunction function)
        {
            if (_functions.ContainsKey(functionName))
            {
                MornLuaLogger.LogWarning($"{functionName} は既に登録されています。");
                return;
            }

            _functions[functionName] = function;
        }

        public void AddDefaultModule(string path)
        {
            if (_requiredModules.Contains(path))
            {
                MornLuaLogger.LogWarning($"{path} は既に登録されています。");
                return;
            }

            _requiredModules.Add(path);
        }

        private async UniTask<LuaState> SetUpLuaState(CancellationToken ct)
        {
            var state = LuaState.Create();
            state.OpenStandardLibraries();

            // ModuleLoaderが指定されている場合は設定、nullの場合はデフォルト（Addressables）
            if (_moduleLoader != null)
            {
                state.ModuleLoader = _moduleLoader;
            }

            // requireの登録
            foreach (var module in _requiredModules)
            {
                await state.DoStringAsync($"require(\"{module}\")", cancellationToken: ct);
            }

            // 関数の登録
            foreach (var (functionName, function) in _functions)
            {
                state.Environment[functionName] = function;
            }

            return state;
        }

        public async UniTask DoStringAsync(string source,
            Func<LuaState, CancellationToken, UniTask> preExecution = null, CancellationToken ct = default)
        {
            var state = await SetUpLuaState(ct);
            if (preExecution != null)
            {
                await preExecution(state, ct);
            }

            await state.DoStringAsync(source, cancellationToken: ct);
        }

        public async UniTask DoFileAsync(string path, Func<LuaState, CancellationToken, UniTask> preExecution = null,
            CancellationToken ct = default)
        {
            var state = await SetUpLuaState(ct);
            if (preExecution != null)
            {
                await preExecution(state, ct);
            }

            await state.DoFileAsync(path, cancellationToken: ct);
        }

        public async UniTask DoFileAsync(LuaAsset luaAsset,
            Func<LuaState, CancellationToken, UniTask> preExecution = null, CancellationToken ct = default)
        {
            var state = await SetUpLuaState(ct);
            if (preExecution != null)
            {
                await preExecution(state, ct);
            }

            await state.DoStringAsync(luaAsset.Text, cancellationToken: ct);
        }
    }
}