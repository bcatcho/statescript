using Transition;

namespace Tests
{
   /// <summary>
   /// A helper class to produce actions with specific results
   /// </summary>
   internal class TestAction : Action<TestMachineContext>
   {
      public TickResult Result;

      public System.Action TickFunc;

      [DefaultParameter]
      public int TestProperty1 { get; set; }

      public string TestProperty2 { get; set; }

      public TestAction()
      {
      }

      public TestAction(TickResult result)
      {
         Result = result;
      }

      public TestAction(TickResult result, System.Action tickFunc)
      {
         Result = result;
         TickFunc = tickFunc;
      }

      protected override TickResult OnTick(TestMachineContext context)
      {
         if (TickFunc != null) {
            TickFunc.Invoke();
         }
         return Result;
      }
   }
}
