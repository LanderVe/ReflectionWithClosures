using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace ConsoleApp2
{
  class Program
  {
    private const int iterations = 100000000;

    static void Main(string[] args)
    {
      var d = new Dummy();
      object o = d;
      var propName = "Text";
      Action<string> action;
      Stopwatch sw = new Stopwatch();

      ////test1, hard-coded
      //action = val => d.Text = val;
      //sw.Restart();

      //for (int i = 0; i < iterations; i++)
      //{
      //  action.Invoke("value1");
      //}

      //sw.Stop();
      //Console.WriteLine(sw.ElapsedMilliseconds);
      //Console.WriteLine(d.Text);

      ////test2 with Reflection
      //action = val => o.GetType().GetProperty(propName).SetValue(o, val);
      //sw.Restart();

      //for (int i = 0; i < iterations; i++)
      //{
      //  action.Invoke("value2");
      //}

      //sw.Stop();
      //Console.WriteLine(sw.ElapsedMilliseconds);
      //Console.WriteLine(d.Text);

      ////test3 with dynamic
      //dynamic dy = o;
      //action = val => dy.Text = val;
      //sw.Restart();

      //for (int i = 0; i < iterations; i++)
      //{
      //  action.Invoke("value3");
      //}

      //sw.Stop();
      //Console.WriteLine(sw.ElapsedMilliseconds);
      //Console.WriteLine(d.Text);


      ////test4, compiled setter
      Action<object, string> setter = GetCompiledSetter(o, propName);
      action = val => setter(o, val);
      sw.Start();

      for (int i = 0; i < iterations; i++)
      {
        action.Invoke("value4");
      }

      sw.Stop();
      Console.WriteLine(sw.ElapsedMilliseconds);
      Console.WriteLine(d.Text);


      ////test5
      //action = GetCompiledLambda<object, string>(o, propName);
      //sw.Restart();

      //for (int i = 0; i < iterations; i++)
      //{
      //  action.Invoke($"value5");
      //}

      //sw.Stop();
      //Console.WriteLine(sw.ElapsedMilliseconds);
      //Console.WriteLine(d.Text);

      ////test6 with factory
      //action = GetCompiledLambdaWithFactory<string>(o, propName);
      //sw.Restart();

      //for (int i = 0; i < iterations; i++)
      //{
      //  action.Invoke($"value6");
      //}

      //sw.Stop();
      //Console.WriteLine(sw.ElapsedMilliseconds);
      //Console.WriteLine(d.Text);

      //reverse engineering
      //Action<string> exp = val => d.Text = val;
      //Expression<Action<string>> exp = val => d.Text = val;
      //Analyse(val => d.Text = val);
    }


    //(object target, string value) => ((Dummy)target).Text = value 
    private static Action<T, string> GetCompiledSetter<T>(T d, string propName)
    {
      var pi = d.GetType().GetProperty(propName);
      ParameterExpression targetExp = Expression.Parameter(typeof(T), "target");
      ParameterExpression valueExp = Expression.Parameter(typeof(string), "value");

      Expression convertExpr = Expression.Convert(targetExp, d.GetType());

      MemberExpression propExp = Expression.Property(convertExpr, pi);
      BinaryExpression assignExp = Expression.Assign(propExp, valueExp);

      var setter = Expression.Lambda<Action<T, string>>
          (assignExp, targetExp, valueExp).Compile();

      return setter;
    }

    //val => obj.Text = val 
    private static Action<TValue> GetCompiledLambda<TTarget, TValue>(TTarget d, string propName)
    {
      var pi = d.GetType().GetProperty(propName);
      ParameterExpression valueExp = Expression.Parameter(typeof(TValue), "val");

      MemberExpression propExp = Expression.Property(Expression.Constant(d), pi); //TODO use Quote? //http://stackoverflow.com/questions/3716492/what-does-expression-quote-do-that-expression-constant-can-t-already-do
      BinaryExpression assignExp = Expression.Assign(propExp, valueExp);

      var lamb = Expression.Lambda<Action<TValue>>
          (assignExp, valueExp).Compile();
      Console.WriteLine(Expression.Lambda<Action<TValue>>(assignExp, valueExp));

      return lamb;
    }

    //(TTarget d) => (TValue val) => target.Text = val 
    private static Action<TValue> GetCompiledLambdaWithFactory<TValue>(object target, string propName)
    {
      var targetExp = Expression.Parameter(target.GetType(), "t"); //not using TTarget, want specific type
      var valueExp = Expression.Parameter(typeof(TValue), "v");

      var pi = target.GetType().GetProperty(propName);
      MemberExpression propExp = Expression.Property(targetExp, pi);

      var lamb = Expression.Lambda(
                  Expression.Lambda<Action<TValue>>(
                    Expression.Assign(propExp, valueExp),
                valueExp),
              targetExp);

      Console.WriteLine(lamb);

      //var factory = (Func<object, Action<string>>)lamb.Compile(); //can't convert Func<Dummy, Action<string>> to Func<Object, Action<string>>
      //var inner = factory.Invoke(d);

      var factory = lamb.Compile();
      var inner = (Action<TValue>)factory.DynamicInvoke(target);

      return inner;
    }


  }
}