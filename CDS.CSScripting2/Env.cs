using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace CDS.CSScripting2
{
    public class Env
    {
        private ImmutableList<string> namespaceNames;
        //private ImmutableList<string> referenceNames;
        private ImmutableList<Assembly> references;
        private Type globalType;
        private static Env defaultInstance;
        private static bool isNetFramework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains(".NET Framework");


        public static Env Default => defaultInstance;



        public static bool IsNetFramework => isNetFramework;



        public IEnumerable<string> NamespaceNames => namespaceNames;


        //public IEnumerable<string> ReferenceNames => referenceNames;

        public IEnumerable<Assembly> References => references;


        public Type GlobalType => globalType;


        static Env()
        {
            var defaultNamespaces = (new[] { typeof(object).Namespace }).ToImmutableList();

            var defaultReferences = new[]
            {
                typeof(object).Assembly,
                typeof(Console).Assembly,
            }.ToImmutableList();

            defaultInstance = new Env(defaultNamespaces, defaultReferences, null);
        }


        private Env(ImmutableList<string> namespaceNames, ImmutableList<Assembly> references, Type globalType)
        {
            this.namespaceNames = namespaceNames.Distinct().ToImmutableList();
            this.references = references.Distinct().ToImmutableList();
            this.globalType = globalType;
        }


        public Env WithAdditionalNamespaceType(Type type)
        {
            return new Env(
                namespaceNames.Add(type.Namespace),
                references,
                globalType);
        }


        public Env WithDrawingReferences()
        {
            if (IsNetFramework)
            {
                // For .NetFramework this requires the System.Drawing assembly, but calling
                // Assembly.Load("System.Drawing") will not work - not sure why! Instead,
                // we need to use the full name of the assembly.
                var systemDrawingFullName = typeof(System.Drawing.Point).Assembly;

                return new Env(
                    namespaceNames,
                    references.Add(systemDrawingFullName),
                    globalType);
            }

            var newReferenceNames = new[] { "System.Drawing", "System.Drawing.Primitives" };
            var newAssemblies = newReferenceNames.Select(Assembly.Load);

            return new Env(
                namespaceNames: namespaceNames,
                references: references.AddRange(newAssemblies),
                globalType);
        }

        public Env WithAdditionalNamespaceName(string namespaceName)
        {
            return new Env(
                namespaceNames.Add(namespaceName),
                references,
                globalType);
        }


        public Env WithAdditionalReferenceName(string referenceName)
        {
            var assembly = Assembly.Load(referenceName);

            return new Env(
                namespaceNames,
                references.Add(assembly),
                globalType);
        }


        public Env WithGlobalType(Type globalType)
        {
            return new Env(
                namespaceNames,
                references,
                globalType);
        }

        public Env WithGlobalType<T>()
        {
            return new Env(
                namespaceNames,
                references,
                typeof(T));
        }

        public Env WithAdditionalReferenceForType<T>()
        {
            return WithAdditionalReferenceName(typeof(T).Assembly.FullName);
        }

        public Env WithAdditionalNamespaceForType<T>()
        {
            return WithAdditionalNamespaceName(typeof(T).Namespace);
        }
    }
}
