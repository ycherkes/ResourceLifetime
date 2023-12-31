using Moq;
using ResourceLifetime.Disposables;

namespace ResourceLifetime.UnitTests
{
    public class DisposableGroupTests
    {
        [Fact]
        public async Task CallsDisposeAsyncMethodOnly_WhenTypeImplementsBoth()
        {
            // Arrange
            var disposable = new Mock<IDisposable>();
            disposable.Setup(df => df.Dispose());
            var asyncDisposable = disposable.As<IAsyncDisposable>();
            asyncDisposable.Setup(df => df.DisposeAsync());

            var disposableGroup = new DisposableGroup 
            { 
                asyncDisposable.Object
            };

            // Act
            await disposableGroup.DisposeAsync();

            // Assert
            Assert.IsAssignableFrom<IAsyncDisposable>(asyncDisposable.Object);
            Assert.IsAssignableFrom<IDisposable>(asyncDisposable.Object);

            asyncDisposable.Verify(d => d.DisposeAsync());
            disposable.Verify(d => d.Dispose(), Times.Never);
        }

        [Fact]
        public void CallsDisposeMethodOnly_WhenTypeImplementsBoth()
        {
            // Arrange
            var disposable = new Mock<IDisposable>();
            disposable.Setup(df => df.Dispose());
            var asyncDisposable = disposable.As<IAsyncDisposable>();
            asyncDisposable.Setup(df => df.DisposeAsync());

            var disposableGroup = new DisposableGroup
            {
                asyncDisposable.Object
            };

            // Act
            disposableGroup.Dispose();

            // Assert
            Assert.IsAssignableFrom<IAsyncDisposable>(asyncDisposable.Object);
            Assert.IsAssignableFrom<IDisposable>(asyncDisposable.Object);

            disposable.Verify(d => d.Dispose());
            asyncDisposable.Verify(d => d.DisposeAsync(), Times.Never);
        }

        [Fact]
        public void ThrowsInvalidOperationException_WhenSynchronouslyDisposingAsyncDisposable()
        {
            // Arrange
            var asyncDisposable = new Mock<IAsyncDisposable>();
            asyncDisposable.Setup(df => df.DisposeAsync());

            var disposableGroup = new DisposableGroup(throwExceptions: true)
            {
                asyncDisposable.Object
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => disposableGroup.Dispose());
            asyncDisposable.Verify(d => d.DisposeAsync(), Times.Never);
        }

        [Fact]
        public void SkipsDisposingSilently_WhenSynchronouslyDisposingAsyncDisposable()
        {
            // Arrange
            var asyncDisposable = new Mock<IAsyncDisposable>();
            asyncDisposable.Setup(df => df.DisposeAsync());

            var disposableGroup = new DisposableGroup(throwExceptions: false)
            {
                asyncDisposable.Object
            };

            // Act
            disposableGroup.Dispose();

            // Assert
            asyncDisposable.Verify(d => d.DisposeAsync(), Times.Never);
        }

        private event EventHandler? MyEvent;

        [Fact]
        public void UnsubscribeViaDispose()
        {
            static void MyEventHandler(object? sender, EventArgs e)
            {
            }

            MyEvent += MyEventHandler;

            // Arrange
            var disposable = Disposable.Create(() => MyEvent -= MyEventHandler);

            var disposableGroup = new DisposableGroup
            {
                disposable
            };

            // Act
            disposableGroup.Dispose();

            // Assert
            Assert.Null(MyEvent);
        }
    }
}