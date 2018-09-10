namespace Pixel3D.Serialization.Demo.TestTypes
{
    [SerializationRoot]
    class BaseClass
    {
    }

    class DerivedClassA : BaseClass
    {
    }

    class DerivedClassB : BaseClass
    {
    }

    class DerivedClassC : DerivedClassA
    {
    }
}
