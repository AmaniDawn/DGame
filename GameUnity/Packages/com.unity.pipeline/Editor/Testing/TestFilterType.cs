namespace Unity.Pipeline.Editor.Testing
{
    /// <summary>
    /// What a test run's filter is matched against.
    /// </summary>
    enum TestFilterType
    {
        /// <summary>Case-insensitive partial match on the test's full name.</summary>
        TestName,
        /// <summary>Case-insensitive partial match on the test's assembly name.</summary>
        Assembly,
        /// <summary>Case-insensitive exact match on one of the test's categories.</summary>
        Category
    }
}
