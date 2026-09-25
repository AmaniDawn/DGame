#if ENABLE_INPUT_SYSTEM
using NUnit.Framework;
using Unity.Pipeline.Runtime.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="RuntimeInputCommand"/> input injection. Inherits <see cref="InputTestFixture"/>,
    /// which saves the real input state on setup and restores it on teardown, so adding virtual devices and
    /// pumping the Input System here never leaks into the editor session. Only compiled when the Input System
    /// is present and active (ENABLE_INPUT_SYSTEM) — the same guard the command uses.
    /// </summary>
    public class RuntimeInputCommandTests : InputTestFixture
    {
        [Test]
        public void SimulateKey_Down_PressesKey()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            var result = RuntimeInputCommand.SimulateKey("A", "down");

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(keyboard.aKey.isPressed, "key should be held after a 'down' action");
        }

        [Test]
        public void SimulateKey_Up_ReleasesKey()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            RuntimeInputCommand.SimulateKey("A", "down");
            Assert.IsTrue(keyboard.aKey.isPressed);

            var result = RuntimeInputCommand.SimulateKey("A", "up");

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsFalse(keyboard.aKey.isPressed, "key should be released after an 'up' action");
        }

        [Test]
        public void SimulateKey_UnknownKey_Fails()
        {
            InputSystem.AddDevice<Keyboard>();

            var result = RuntimeInputCommand.SimulateKey("NotARealKey", "down");

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
        }

        [Test]
        public void SimulateKey_NoKeyboard_Fails()
        {
            // No keyboard device added in this isolated fixture state.
            var result = RuntimeInputCommand.SimulateKey("A", "down");

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
        }

        [Test]
        public void SimulatePointer_Move_UpdatesPosition()
        {
            var mouse = InputSystem.AddDevice<Mouse>();

            var result = RuntimeInputCommand.SimulatePointer(123f, 456f, "move");

            Assert.IsTrue(result.Success, result.Error);
            var pos = mouse.position.ReadValue();
            Assert.AreEqual(123f, pos.x, 0.5f);
            Assert.AreEqual(456f, pos.y, 0.5f);
        }

        [Test]
        public void SimulatePointer_Down_PressesButtonAtPosition()
        {
            var mouse = InputSystem.AddDevice<Mouse>();

            var result = RuntimeInputCommand.SimulatePointer(10f, 20f, "down", "left");

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(mouse.leftButton.isPressed, "left button should be held after a 'down' action");
        }
    }
}
#endif
