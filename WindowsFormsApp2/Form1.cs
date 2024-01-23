using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using static WindowsFormsApp2.Dog;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1(Dog dog)
        {
                
        }
        public Form1()
        {
            InitializeComponent();
            var banana1 = new Banana
            {
                ChildBananas = new Dictionary<string, Banana>
            {
                { "Child1", new Banana
                    {
                        ChildBananas = new Dictionary<string, Banana>
                        {
                            { "GrandChild1", new Banana { } },
                            { "GrandChild2", new Banana
                                {
                                    ChildBananas = new Dictionary<string, Banana>
                                    {
                                        { "GreatGrandChild1", new Banana { } }
                                    }
                                }
                            }
                        }
                    }
                },
                { "Child2", new Banana
                    {
                        ChildBananas = new Dictionary<string, Banana>
                        {
                            { "GrandChild3", new Banana { } }
                        }
                    }
                }
            }
            };
            var reflectionEmitExample = new ReflectionEmitExample();
            var dynamicObj= new DynamicObj() { Banana=banana1};
            var dynamicObjNew = reflectionEmitExample.CloneObjectWithModifiedProperty(dynamicObj);

            //this works:
            propertyGrid1.SelectedObject = dynamicObjNew;
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            propertyGrid1.SelectedObject = e.ChangedItem;
        }
    }

    public class ReflectionEmitExample
    {
        public object CloneObjectWithModifiedProperty(object input)
        {
            Type inputType = input.GetType();
            PropertyInfo[] properties = inputType.GetProperties();

            AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);

            foreach (var prop in properties)
            {
                CreateProperty(typeBuilder, prop, prop.GetCustomAttributes());
            }

            Type newType = typeBuilder.CreateType();
            object newInstance = Activator.CreateInstance(newType);

            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(Banana))
                {
                    Dog dogValue = ConvertBananaToDog((Banana)prop.GetValue(input));
                    newType.GetProperty(prop.Name).SetValue(newInstance, dogValue);
                }
                else
                {
                    var value = prop.GetValue(input);
                    newType.GetProperty(prop.Name).SetValue(newInstance, value);
                }
            }

            return newInstance;
        }

        private void CreateProperty(TypeBuilder typeBuilder, PropertyInfo prop, IEnumerable<Attribute> attributes)
        {
            Type propType = prop.PropertyType;
            if (propType == typeof(Banana))
            {
                propType = typeof(Dog);
            }
            string propName = prop.Name;
            var attributesArray= prop.GetCustomAttributes();
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propName.ToLower(), propType, FieldAttributes.Private);
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propName, System.Reflection.PropertyAttributes.HasDefault, propType, null);
            //foreach (var attr in attributesArray)
            //{
            //    Type attrType = attr.GetType();
            //    ConstructorInfo attrCtor = attrType.GetConstructor(Type.EmptyTypes);
            //    if (attrCtor != null)
            //    {
            //        CustomAttributeBuilder customAttrBuilder = new CustomAttributeBuilder(attrCtor, new object[] { });
            //        propertyBuilder.SetCustomAttribute(customAttrBuilder);
            //    }
            //}
                MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod("get_" + propName, MethodAttributes.Public | MethodAttributes.SpecialName, propType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr = typeBuilder.DefineMethod("set_" + propName, MethodAttributes.Public | MethodAttributes.SpecialName, null, new Type[] { propType });
            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }

        private Dog ConvertBananaToDog(Banana banana)
        {
            var dog = new Dog(banana);
            if (banana.ChildBananas != null)
            {
                var bananaDict = new Dictionary<string, Banana>();
                foreach (var kvp in ConvertBananaDictToDogDict(banana.ChildBananas))
                {
                    bananaDict[kvp.Key] = kvp.Value; // Dog is implicitly cast to Banana here
                }
                dog.ChildBananas = bananaDict;
            }
            // Copy other properties from Banana to Dog, if needed
            return dog;
        }

        private Dictionary<string, Dog> ConvertBananaDictToDogDict(Dictionary<string, Banana> bananaDict)
        {
            var dogDict = new Dictionary<string, Dog>();
            foreach (var kvp in bananaDict)
            {
                dogDict[kvp.Key] = ConvertBananaToDog(kvp.Value);
            }
            return dogDict;
        }
    }

    public class DynamicObj
    {
        public Banana Banana { get; set; }
    }

    public class Banana
    {
        public Dictionary<string, Banana> ChildBananas { get; set; }

        public Banana(Banana banana)
        {
            
        }

        public Banana()
        {
            // Default constructor
        }
    }
    [Editor(typeof(DogEditor), typeof(UITypeEditor))]

    public class Dog : Banana
    {
        public Dog(Banana banana) : base(banana)
        {
            // Initialize or convert additional properties if necessary
        }

        public Dog()
        {
            // Default constructor
        }

        public class DynamicObj
        {
            public Banana Banana { get; set; }
             

        }
        public class DogEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
            {
                // This will specify that the editor is modal
                return UITypeEditorEditStyle.Modal;
            }

            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                IWindowsFormsEditorService editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (editorService != null && value is Dog)
                {
                     editorService.ShowDialog(new Form1(value as Dog));
                }
                return value; // Return the edited object
            }
        }
    }
}
