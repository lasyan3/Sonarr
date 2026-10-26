import React, { useCallback, useState } from 'react';
import Alert from 'Components/Alert';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import useApiQuery from 'Helpers/Hooks/useApiQuery';
import { inputTypes, kinds } from 'Helpers/Props';
import { AlternateTitle } from 'Series/Series';
import { InputChanged } from 'typings/inputs';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import AlternateTitleRow from './AlternateTitleRow';
import styles from './EditAlternateTitlesModalContent.css';

const columns: Column[] = [
  {
    name: 'title',
    label: () => translate('Title'),
    isVisible: true,
  },
  {
    name: 'seasonNumber',
    label: () => translate('Season'),
    isVisible: true,
  },
  {
    name: 'sceneSeasonNumber',
    label: () => translate('Scene'),
    isVisible: true,
  },
  {
    name: 'source',
    label: () => translate('Source'),
    isVisible: true,
  },
  {
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

interface ApiError {
  responseJSON?: { message?: string };
}

function sendRequest(options: object) {
  return new Promise<void>((resolve, reject) => {
    const { request } = createAjaxRequest(options);

    request
      .done(() => resolve())
      .fail((xhr: ApiError) =>
        reject(
          xhr.responseJSON?.message ?? translate('AlternateTitlesSaveError')
        )
      );
  });
}

export interface EditAlternateTitlesModalContentProps {
  seriesId: number;
  seriesTitle: string;
  onAlternateTitlesChange(): void;
  onModalClose(): void;
}

function EditAlternateTitlesModalContent({
  seriesId,
  seriesTitle,
  onAlternateTitlesChange,
  onModalClose,
}: EditAlternateTitlesModalContentProps) {
  const [newTitle, setNewTitle] = useState('');
  const [newSeasonNumber, setNewSeasonNumber] = useState<number | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const { isLoading, error, data, refetch } = useApiQuery<AlternateTitle[]>({
    url: `/alternatetitle?seriesId=${seriesId}`,
  });

  const runRequest = useCallback(
    (options: object) => {
      setIsSaving(true);
      setSaveError(null);

      return sendRequest(options)
        .then(() => {
          onAlternateTitlesChange();
          return refetch();
        })
        .then(() => true)
        .catch((message: string) => {
          setSaveError(message);
          return false;
        })
        .finally(() => setIsSaving(false));
    },
    [onAlternateTitlesChange, refetch]
  );

  const handleTitleChange = useCallback(({ value }: InputChanged<string>) => {
    setNewTitle(value);
  }, []);

  const handleSeasonNumberChange = useCallback(
    ({ value }: InputChanged<number | null>) => {
      setNewSeasonNumber(value);
    },
    []
  );

  const handleAddPress = useCallback(() => {
    runRequest({
      url: '/alternatetitle',
      method: 'POST',
      dataType: 'json',
      data: JSON.stringify({
        seriesId,
        title: newTitle,
        seasonNumber: newSeasonNumber,
      }),
    }).then((success) => {
      if (success) {
        setNewTitle('');
        setNewSeasonNumber(null);
      }
    });
  }, [seriesId, newTitle, newSeasonNumber, runRequest]);

  const handleDeletePress = useCallback(
    (id: number) => {
      runRequest({
        url: `/alternatetitle/${id}`,
        method: 'DELETE',
      });
    },
    [runRequest]
  );

  const handleExcludePress = useCallback(
    (title: string, isExcluded: boolean) => {
      runRequest({
        url: '/alternatetitle/exclusion',
        method: 'PUT',
        dataType: 'json',
        data: JSON.stringify({ seriesId, title, isExcluded }),
      });
    },
    [seriesId, runRequest]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('EditAlternateTitlesModalHeader', { title: seriesTitle })}
      </ModalHeader>

      <ModalBody>
        <Alert kind={kinds.INFO}>{translate('EditAlternateTitlesInfo')}</Alert>

        {data?.some((alternateTitle) => alternateTitle.isIgnored) ? (
          <Alert kind={kinds.WARNING}>
            {translate('IgnoreAlternateTitlesEnabledInfo')}
          </Alert>
        ) : null}

        {isLoading ? <LoadingIndicator /> : null}

        {!isLoading && error ? (
          <Alert kind={kinds.DANGER}>
            {translate('AlternateTitlesLoadError')}
          </Alert>
        ) : null}

        {!isLoading && !error && data && !data.length ? (
          <Alert kind={kinds.INFO}>{translate('NoAlternateTitles')}</Alert>
        ) : null}

        {!isLoading && !error && data && data.length ? (
          <Table columns={columns}>
            <TableBody>
              {data.map((alternateTitle) => (
                <AlternateTitleRow
                  key={alternateTitle.id}
                  alternateTitle={alternateTitle}
                  isSaving={isSaving}
                  onDeletePress={handleDeletePress}
                  onExcludePress={handleExcludePress}
                />
              ))}
            </TableBody>
          </Table>
        ) : null}

        <div className={styles.addForm}>
          <FormGroup>
            <FormLabel>{translate('AddAlternateTitle')}</FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT}
              name="title"
              value={newTitle}
              helpText={translate('AddAlternateTitleHelpText')}
              onChange={handleTitleChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('SeasonNumber')}</FormLabel>

            <FormInputGroup
              type={inputTypes.NUMBER}
              name="seasonNumber"
              value={newSeasonNumber}
              min={0}
              helpText={translate('AlternateTitleSeasonNumberHelpText')}
              onChange={handleSeasonNumberChange}
            />
          </FormGroup>
        </div>

        {saveError ? <Alert kind={kinds.DANGER}>{saveError}</Alert> : null}
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>

        <SpinnerButton
          kind={kinds.PRIMARY}
          isSpinning={isSaving}
          isDisabled={!newTitle.trim()}
          onPress={handleAddPress}
        >
          {translate('Add')}
        </SpinnerButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default EditAlternateTitlesModalContent;
