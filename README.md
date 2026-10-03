### Multithreaded Telecom Call Processing Pipeline (Assignment 2)

 How to Build and Run
1. To run the main console application: dotnet run (from the main project directory).
2. To run the automated tests: dotnet test (from the solution root or Tests directory).

 Architecture: Pure vs Impure Components
* Pure Component: CallPricing.CalculateCost is a pure function. It relies entirely on immutable input parameters, avoids state mutation, performs no I/O operations (like Console.WriteLine), and consistently returns the same output for the same input.
* Impure Components: The ProcessCallsParallel method is impure because it relies on OS-level thread creation and interacts with system execution state. Additionally, writing to the allocated output arrays (out1 and out2) is a mutation operation, even though it is isolated. Any Console I/O used to display the results in Program.cs is also impure.

### The Data Race Condition
Using globalCallCounter++ in a concurrent environment causes a race condition because the increment is not atomic. It represents a read-modify-write cycle.
* Lost-update interleaving scenario: Thread A and Thread B simultaneously read the counter value (e.g., 5). Thread A increments its local copy to 6 and prepares to write it back. Thread B also increments its local copy to 6. Both threads write 6 back to the shared memory. Instead of the counter properly updating to 7, one update is overwritten and lost.

### Immutability and Validation
The readonly record struct provides immutability, ensuring that fields cannot be modified after initialization. However, readonly does not guarantee that every instance is valid, because structs can be instantiated bypassing the constructor using default(CallRecord). To address this, the CalculateCost function implements defensive checking to explicitly reject invalid default states.

### Partitioning Rationale
Instead of using synchronization primitives like lock or Interlocked (which introduce overhead and potential bottlenecks), the pipeline uses array partitioning. By splitting the input array and allocating independent, isolated output arrays for each thread, we ensure that each worker thread only mutates its own disjoint segment of memory. Calling Thread.Join() guarantees that both threads have completely finished processing and safely flushed their writes before the main thread performs the final sequential aggregation.

### Limitations
* Raw threading (Thread.Start()) introduces significant OS overhead. For very small arrays, ProcessCallsSequentia will likely perform faster than ProcessCallsParallel` due to the cost of thread context switching and allocation.
* The application is strictly designed for even-length arrays as per assignment constraints.