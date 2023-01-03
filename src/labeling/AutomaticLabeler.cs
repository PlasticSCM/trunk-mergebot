﻿using System;
using System.Threading.Tasks;
using Codice.CM.Server.Devops;

namespace TrunkBot.Labeling
{
    public static partial class AutomaticLabeler
    {
        internal class Result
        {
            internal bool IsSuccessful;
            internal string Name;
            internal string ErrorMessage;

            internal Result(bool bSuccessful, string name, string errorMessage)
            {
                IsSuccessful = bSuccessful;
                Name = name;
                ErrorMessage = errorMessage;
            }
        }

        async internal static Task<Result> CreateLabel(
            IRepositoryOperationsForMergebot repoApi,
            int csetId, 
            string repository, 
            string labelPattern, 
            DateTime now)
        {
            if (string.IsNullOrEmpty(labelPattern))
                return new Result(false, string.Empty, Messages.NO_PATTERN_SPECIFIED); 

            string plasticFindPattern = PatternTranslator.ToFindPattern(labelPattern, now);

            if (string.IsNullOrEmpty(plasticFindPattern))
                return new Result(false, string.Empty, Messages.MALFORMED_PATTERN);

            Label lastMatchingLabel = await repoApi.FindMostRecentLabel(
                repository, now.AddYears(-2), plasticFindPattern);

            string newLabelNameCandidate = NewLabelNameGenerator.GetNewLabelName(
                plasticFindPattern, lastMatchingLabel);

            if (string.IsNullOrEmpty(newLabelNameCandidate))
                return new Result(
                    false,
                    string.Empty,
                    string.Format(
                        Messages.CANNOT_CALCULATE_NAME_CANDIDATE,
                        plasticFindPattern));

            string newLabelName = await PickNonExistentLabelName(
                repoApi, repository, newLabelNameCandidate);

            if (string.IsNullOrEmpty(newLabelName))
                return new Result(
                    false, 
                    string.Empty, 
                    string.Format(
                        Messages.CANNOT_CALCULATE_NAME_CANDIDATE_AUTO, 
                        newLabelName,
                        newLabelName + Tokens.AUTO_INCREMENT_EXISTING_PATTERN));

            return CreateLabelInRep(repoApi, newLabelName, csetId, repository);
        }

        static Result CreateLabelInRep(
            IRepositoryOperationsForMergebot repoApi,
            string labelName,
            int csetId,
            string repository)
        {
            try
            {
                repoApi.CreateLabel(repository, labelName, csetId, string.Empty);
                return new Result(true, labelName, string.Empty);
            }
            catch(Exception e)
            {
                return new Result(false, labelName, e.Message);
            }
        }

        async static Task<string> PickNonExistentLabelName(
            IRepositoryOperationsForMergebot repoApi, string repository, string newLabelNameCandidate)
        {
            Label repoLabel = await repoApi.FindMostRecentLabel(
                repository, DateTime.MinValue, newLabelNameCandidate);

            if (repoLabel == null)
                return newLabelNameCandidate;

            return await GetAutoIncrementedExistingLabel(repoApi, repository, newLabelNameCandidate);
        }

        async static Task<string> GetAutoIncrementedExistingLabel(
            IRepositoryOperationsForMergebot repoApi, string repository, string existingLabelName)
        {
            Label lastMatchingLabel = await repoApi.FindMostRecentLabel(
                repository, 
                DateTime.MinValue,
                existingLabelName + Tokens.AUTO_INCREMENT_EXISTING_PATTERN);

            return NewLabelNameGenerator.GetNewLabelName(
                existingLabelName + Tokens.AUTO_INCREMENT_EXISTING_PATTERN, lastMatchingLabel);
        }

        internal static class Messages
        {
            internal const string NO_PATTERN_SPECIFIED =
                "No automatic label pattern was specified. Hence, no label will be created.";

            internal const string MALFORMED_PATTERN =
                "The specified automatic label pattern is not valid. " +
                "Please review the specified pattern and its variable declarations. " +
                "E.g. 'Rel_1${AUTO_INCREMENT_NUMBER}'";

            internal const string CANNOT_CALCULATE_NAME_CANDIDATE =
                "An unexpected error occurred while calculating the new auto-incremented " +
                "label name with pattern [{0}].";

            internal const string CANNOT_CALCULATE_NAME_CANDIDATE_AUTO =
                "Despite the calculated label [{0}] already exists, mergebot failed to calculate " +
                "a new auto-generated label name with pattern [{1}].";
        }

    }
}

