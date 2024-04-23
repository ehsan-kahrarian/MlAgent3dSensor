using System;
using System.Collections.Generic;
using MBaske.Sensors.Grid;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class SphereAgentReppel : Agent

{
    [SerializeField] private GridSensorComponent3D sensorComponent;
    
    private IList<GameObject> targets;
    [SerializeField] private GameObject tag;
    [SerializeField] private Transform reppelGameObject;
    
    [SerializeField] private float moveSpeed = 3f;
    
    [SerializeField]private float targetFollowAngle = 30;
    [SerializeField]private float targetFollowDistance = 50;
    private float targetFollowDistanceSqr;
    private float timePenalty = .01f;
    private float _timer = 0f;

    
    public override void Initialize()
    {
        targets = new List<GameObject>(10);
        targetFollowDistanceSqr = targetFollowDistance * targetFollowDistance;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.eulerAngles);
        
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        
        foreach (var target in sensorComponent.GetDetectedGameObjects(tag.tag))
        {
            Vector3 delta = target.transform.position - pos;
            if (Vector3.Angle(fwd, delta) < targetFollowAngle && 
                delta.sqrMagnitude < targetFollowDistanceSqr)
            {
                targets.Add(target);
            }
        }
        
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        
        var moveZ = actions.ContinuousActions[0];
        transform.Translate(new Vector3(0, 0, moveZ) * Time.deltaTime * moveSpeed,Space.Self);
        var rotateY = actions.ContinuousActions[1];
        var rotateZ = actions.ContinuousActions[2];
        transform.Rotate(rotateZ * 50f,rotateY,0);
        Vector3 pos = transform.position;

        foreach (var target in targets)
        {
            Vector3 delta = target.transform.position - pos;
            float distance = Vector3.Distance(target.transform.position, transform.position);
            transform.position = Vector3.MoveTowards(transform.position,target.transform.position,Time.deltaTime * moveSpeed);
            AddReward(distance * 0.01f);
        }

        _timer += Time.fixedDeltaTime;
        AddReward(_timer * timePenalty);
    }

    public override void OnEpisodeBegin()
    {
        _timer = 0f;
        transform.localPosition = Vector3.zero;
        var randomX = Random.Range(-10f, 10f);
        var randomY = Random.Range(-10f, 10f);
        var randomZ = Random.Range(-10f, 10f);
        foreach (var target in sensorComponent.GetDetectedGameObjects(tag.tag))
        {
            if (target == null) {return;}
            
            target.transform.position = new Vector3(randomX, randomY, randomZ);
        }
        reppelGameObject.position = new Vector3(randomX, randomY, randomZ); 
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Vertical");
        continuousActions[1] = Input.GetAxisRaw("Horizontal");
        continuousActions[2] = Input.GetAxisRaw("Mouse Y");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Target>(out Target target))
        {
            SetReward(-1);
            EndEpisode();
        }
        
        if (other.TryGetComponent<Obstacle>(out Obstacle obstacle))
        {
            SetReward(-1);
            EndEpisode();
        }

    }
}