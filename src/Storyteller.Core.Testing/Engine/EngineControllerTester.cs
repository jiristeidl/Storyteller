﻿using System.Collections.Generic;
using System.Linq;
using FubuTestingSupport;
using NUnit.Framework;
using Rhino.Mocks;
using Storyteller.Core.Engine;
using Storyteller.Core.Engine.UserInterface;
using Storyteller.Core.Messages;
using Storyteller.Core.Model.Persistence;

namespace Storyteller.Core.Testing.Engine
{

    [TestFixture]
    public class when_receiving_a_run_spec_message : EngineControllerContext
    {
        protected override void theContextIs()
        {
            ClassUnderTest.Receive(new RunSpec{id = "embeds"});
        }

        [Test]
        public void should_keep_track_of_outstanding_request()
        {
            ClassUnderTest.OutstandingRequests().Single()
                .Node.id.ShouldEqual("embeds");
        }

        [Test]
        public void latches_on_runspec_such_that_it_will_not_double_queue()
        {
            ClassUnderTest.RunSpec("embeds");
            ClassUnderTest.RunSpec("embeds");
            ClassUnderTest.RunSpec("embeds");
            ClassUnderTest.RunSpec("embeds");
            ClassUnderTest.RunSpec("embeds");

            ClassUnderTest.OutstandingRequests().Single()
                .Node.id.ShouldEqual("embeds");
        }

        [Test]
        public void should_broadcast_a_spec_queued_message()
        {
            MockFor<IUserInterfaceObserver>().AssertWasCalled(x => x.SpecQueued(findSpec("embeds")));
        }

        [Test]
        public void should_enqueue_the_request()
        {
            MockFor<ISpecificationEngine>().AssertWasCalled(x => x.Enqueue(new SpecExecutionRequest(findSpec("embeds"), null)));
        }

    }

    [TestFixture]
    public class when_receiving_a_run_specs_message : EngineControllerContext
    {
        protected override void theContextIs()
        {
            Services.PartialMockTheClassUnderTest();

            ClassUnderTest.Expect(x => x.RunSpec("a"));
            ClassUnderTest.Expect(x => x.RunSpec("b"));
            ClassUnderTest.Expect(x => x.RunSpec("c"));

            ClassUnderTest.Receive(new RunSpecs{list = new []{"a", "b", "c"}});
        }

        [Test]
        public void enqueues_each_spec()
        {
            ClassUnderTest.VerifyAllExpectations();
        }
    }

    [TestFixture]
    public class when_receiving_a_request_to_cancel_a_spec : EngineControllerContext
    {
        private SpecExecutionRequest theOutstandingRequest;

        protected override void theContextIs()
        {
            ClassUnderTest.Receive(new RunSpec { id = "embeds" });
            theOutstandingRequest = ClassUnderTest.OutstandingRequests().Single();

            ClassUnderTest.Receive(new CancelSpec{id = "embeds"});
        }

        [Test]
        public void the_outstanding_request_should_be_cancelled()
        {
            theOutstandingRequest.IsCancelled.ShouldBeTrue();
        }

        [Test]
        public void should_be_removed_from_the_outstanding_request_list()
        {
            ClassUnderTest.OutstandingRequests()
                .Any().ShouldBeFalse();
        }
    }

    [TestFixture]
    public class When_receiving_the_request_to_cancel_all_specs : EngineControllerContext
    {
        private IEnumerable<SpecExecutionRequest> theOutstandingRequests;

        protected override void theContextIs()
        {
            ClassUnderTest.Receive(new RunSpec { id = "embeds" });
            ClassUnderTest.Receive(new RunSpec { id = "sentence1" });
            ClassUnderTest.Receive(new RunSpec { id = "sentence2" });
            ClassUnderTest.Receive(new RunSpec { id = "sentence3" });

            theOutstandingRequests = ClassUnderTest.OutstandingRequests();
            theOutstandingRequests.Count().ShouldEqual(4);

            ClassUnderTest.Receive(new CancelAllSpecs());
        }

        [Test]
        public void all_the_outstanding_requests_should_be_canceled()
        {
            theOutstandingRequests.Each(x => x.IsCancelled.ShouldBeTrue());
        }

        [Test]
        public void should_be_no_outstanding_requests()
        {
            ClassUnderTest.OutstandingRequests().Any()
                .ShouldBeFalse();
        }
    }

    public abstract class EngineControllerContext : InteractionContext<EngineController>
    {
        protected sealed override void beforeEach()
        {
            ClassUnderTest.Receive(new HierarchyLoaded{root = TestingContext.Hierarchy});
            theContextIs();
        }

        protected abstract void theContextIs();

        protected SpecNode findSpec(string id)
        {
            return TestingContext.Hierarchy.GetAllSpecs().FirstOrDefault(x => x.id == id);
        }
    }

    
}