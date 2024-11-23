using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace CDS.CSScripting
{
    public class Env
    {
        private ImmutableList<string> namespaceNames;
        private ImmutableList<string> referenceNames;
        private Type globalType;
        private static Env defaultInstance;
        private static bool isNetFramework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains(".NET Framework");


        public static Env Default => defaultInstance;



        public static bool IsNetFramework => isNetFramework;



        public IEnumerable<string> NamespaceNames => namespaceNames;


        public IEnumerable<string> ReferenceNames => referenceNames;


        public Type GlobalType => globalType;


        static Env()
        {
            var defaultNamespaces = (new[] { typeof(object).Namespace }).ToImmutableList();

            var defaultReferences = new[]
            {
                typeof(object).Assembly.FullName,
                typeof(Console).Assembly.FullName,
            }.ToImmutableList();

            defaultInstance = new Env(defaultNamespaces, defaultReferences, null);
        }


        private Env(ImmutableList<string> namespaceNames, ImmutableList<string> referenceNames, Type globalType)
        {
            this.namespaceNames = namespaceNames.Distinct().ToImmutableList();
            this.referenceNames = referenceNames.Distinct().ToImmutableList();
            this.globalType = globalType;
        }


        public Env WithAdditionalNamespaceType(Type type)
        {
            return new Env(
                namespaceNames.Add(type.Namespace),
                referenceNames,
                globalType);
        }


        public Env WithDrawingReferences()
        {
            if (IsNetFramework)
            {
                // For .NetFramework this requires the System.Drawing assembly, but calling
                // Assembly.Load("System.Drawing") will not work - not sure why! Instead,
                // we need to use the full name of the assembly.
                var systemDrawingFullName = typeof(System.Drawing.Point).Assembly.FullName;

                return new Env(
                    namespaceNames,
                    referenceNames.Add(systemDrawingFullName),
                    globalType);
            }

            return new Env(
                namespaceNames,
                referenceNames.AddRange(new[] { "System.Drawing", "System.Drawing.Primitives" }),
                globalType);
        }

        public Env WithAdditionalNamespaceName(string namespaceName)
        {
            return new Env(
                namespaceNames.Add(namespaceName),
                referenceNames,
                globalType);
        }


        public Env WithAdditionalReferenceName(string referenceName)
        {
            return new Env(
                namespaceNames,
                referenceNames.Add(referenceName),
                globalType);
        }


        public Env WithGlobalType(Type globalType)
        {
            return new Env(
                namespaceNames,
                referenceNames,
                globalType);
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
