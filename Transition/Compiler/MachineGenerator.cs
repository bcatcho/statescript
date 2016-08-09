using Transition.Compiler.AstNode;
using System.Reflection;
using System.Collections.Generic;
using System;
using Transition.Compiler.ValueConverters;

namespace Transition.Compiler
{
   /// <summary>
   /// The "code" generator for the compiler. Accepts a validated syntax tree and returns an executable Machine.
   /// </summary>
   public class MachineGenerator<T> where T : Context
   {
      private Dictionary<string, Type> _actionLookupTable;
      private Dictionary<Type, IValueConverter> _valueConverterLookup;
      private HashSet<Assembly> _loadedAssemblies;
      private readonly Dictionary<string, Dictionary<string, PropertyInfo>> _propertyInfoCache;
      private readonly Dictionary<string, PropertyInfo> _defaultPropertyInfoCache;

      public MachineGenerator()
      {
         _actionLookupTable = new Dictionary<string, Type>();
         _valueConverterLookup = new Dictionary<Type, IValueConverter>();
         _loadedAssemblies = new HashSet<Assembly>();
         _propertyInfoCache = new Dictionary<string, Dictionary<string, PropertyInfo>>();
         _defaultPropertyInfoCache = new Dictionary<string, PropertyInfo>();
      }

      /// <summary>
      /// Call before generate to build a lookup table of Actions to instantiate during generation. This method
      /// uses reflection to look for decendants of "Action" and cache anything that is used to instantiate them.
      /// </summary>
      public void Initialize(params Assembly[] assemblies)
      {
         // default assembly
         LoadAssembly(Assembly.GetAssembly(typeof(MachineGenerator<>)));

         if (assemblies != null) {
            foreach (var assembly in assemblies) {
               LoadAssembly(assembly);
            }
         }
      }

      /// <summary>
      /// Generates an executable Machine from a syntax tree
      /// </summary>
      /// <param name="machineAst">Machine syntax tree node</param>
      public Machine<T> Generate(MachineAstNode machineAst)
      {
         var machine = new Machine<T>
         {
            Identifier = machineAst.Identifier,
         };
         machine.EnterAction = GenerateAction(machineAst.Action);

         for (int i = 0; i < machineAst.States.Count; ++i) {
            machine.AddState(GenerateState(machineAst.States[i]));
         }

         return machine;
      }

      private State<T> GenerateState(StateAstNode stateNode)
      {
         var state = new State<T>
         {
            Identifier = stateNode.Identifier,
         };
         if (stateNode.Run != null) {
            for (int i = 0; i < stateNode.Run.Actions.Count; ++i) {
               state.AddRunAction(GenerateAction(stateNode.Run.Actions[i]));
            }
         }
         if (stateNode.Enter != null) {
            for (int i = 0; i < stateNode.Enter.Actions.Count; ++i) {
               state.AddEnterAction(GenerateAction(stateNode.Enter.Actions[i]));
            }
         }
         if (stateNode.Exit != null) {
            for (int i = 0; i < stateNode.Exit.Actions.Count; ++i) {
               state.AddExitAction(GenerateAction(stateNode.Exit.Actions[i]));
            }
         }
         if (stateNode.On != null) {
            for (int i = 0; i < stateNode.On.Actions.Count; ++i) {
               state.AddOnAction(stateNode.On.Actions[i].Message, GenerateAction(stateNode.On.Actions[i]));
            }
         }
         return state;
      }

      private Action<T> GenerateAction(ActionAstNode actionNode)
      {
         var action = CreateInstance(actionNode.Identifier);

         CachePropertyInfos(actionNode.Identifier, action);
         PropertyInfo propInfo;
         ParamAstNode param;
         for (int i = 0; i < actionNode.Params.Count; ++i) {
            param = actionNode.Params[i];
            propInfo = GetPropertyInfo(actionNode.Identifier, param.Identifier);
            propInfo.SetValue(action, ConvertValueTo(param.Val, propInfo.PropertyType), null);
         }

         return action;
      }

      private Action<T> CreateInstance(string actionIdentifier)
      {
         if (!_actionLookupTable.ContainsKey(actionIdentifier)) {
            throw new KeyNotFoundException(string.Format("Could not find action for name [{0}]", actionIdentifier));
         }
         var type = _actionLookupTable[actionIdentifier];
         if (type.IsGenericType) {
            type = type.MakeGenericType(typeof(T));
         }

         return (Action<T>)Activator.CreateInstance(type);
      }

      private object ConvertValueTo(string value, System.Type type)
      {
         if (_valueConverterLookup.ContainsKey(type)) {
            object result;
            _valueConverterLookup[type].TryConvert(value, out result);
            return result;
         }

         return null;
      }

      private void LoadAssembly(Assembly assembly)
      {
         if (!_loadedAssemblies.Contains(assembly)) {
            _loadedAssemblies.Add(assembly);
            LoadValueConverters(assembly);
            LoadActions(assembly);
         }
      }

      private void LoadValueConverters(Assembly assembly)
      {
         var converterType = typeof(IValueConverter);
         var types = assembly.GetTypes();
         IValueConverter converter;
         foreach (var type in types) {
            if (converterType.IsAssignableFrom(type) && !type.IsInterface) {
               converter = (IValueConverter)Activator.CreateInstance(type);
               _valueConverterLookup.Add(converter.GetConverterType(), converter);
            }
         }
      }

      private void LoadActions(Assembly assembly)
      {
         var action = typeof(Transition.Action<>);
         var types = assembly.GetTypes();
         Type genericDef = null;
         string name = null;
         foreach (var type in types) {
            if ((type.BaseType != null && type.BaseType.IsGenericType)) {
               genericDef = type.BaseType.GetGenericTypeDefinition();
               if (genericDef == action) {
                  // lowercase the names and remove generic param
                  name = type.Name.Split('`')[0];
                  AddLookupTableId(name, type);

                  if (type.IsDefined(typeof(AltIdAttribute), true)) {
                     var altIdAttribs = (AltIdAttribute[])type.GetCustomAttributes(typeof(AltIdAttribute), true);
                     foreach (var attrib in altIdAttribs) {
                        foreach (var altId in attrib.AltIds) {
                           AddLookupTableId(altId, type);
                        }
                     }
                  }
               }
            }
         }
      }

      private void AddLookupTableId(string name, Type type)
      {
         name = name.ToLower();
         _actionLookupTable.Add(name, type);
      }

      private void CachePropertyInfos(string objName, object obj)
      {
         if (!_propertyInfoCache.ContainsKey(objName)) {
            var cache = new Dictionary<string, PropertyInfo>();
            foreach (var prop in obj.GetType().GetProperties()) {
               cache.Add(prop.Name.ToLower(), prop);

               if (prop.IsDefined(typeof(DefaultParameterAttribute), false)) {
                  _defaultPropertyInfoCache.Add(objName, prop);
               }
            }
            _propertyInfoCache.Add(objName, cache);
         }
      }

      private PropertyInfo GetPropertyInfo(string objectName, string paramName)
      {
         if (paramName == ParserConstants.DefaultParameterIdentifier) {
            return _defaultPropertyInfoCache[objectName];
         }

         return _propertyInfoCache[objectName][paramName];
      }
   }
}
